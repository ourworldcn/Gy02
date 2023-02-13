using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace OW.Server
{
    public class OwSchedulerEntry
    {
        /// <summary>
        /// 任务的唯一标识对象。
        /// </summary>
        public object Key { get; set; }

        /// <summary>
        /// (键，state)。返回值如果是true则标识成功，否则将在下次扫描时继续执行该任务。
        /// </summary>
        public Func<object, object, bool> TaskCallback { get; set; }

        /// <summary>
        /// 任务回调的第二个参数。
        /// </summary>
        public object State { get; set; }

        /// <summary>
        /// 调用任务的周期。
        /// </summary>
        /// <value>可以是<see cref="Timeout.InfiniteTimeSpan"/>或任何大于0的间隔。</value>
        public TimeSpan Period { get; set; }

        /// <summary>
        /// 上次执行的时间点。
        /// </summary>
        public DateTime LastUtc { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void IsValid()
        {
            if (Period == TimeSpan.Zero)
                throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class OwSchedulerOptions : IOptions<OwSchedulerOptions>
    {
        public OwSchedulerOptions()
        {

        }

        /// <summary>
        /// 默认的任务执行频度。
        /// </summary>
        public TimeSpan Frequency { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 设置或获取锁定键的回调。应支持递归与<see cref="UnlockCallback"/>配对使用。
        /// 默认值是<see cref="SingletonLocker.TryEnter(object, TimeSpan)"/>。
        /// </summary>
        public Func<object, TimeSpan, bool> LockCallback { get; set; } = SingletonLocker.TryEnter;

        /// <summary>
        /// 设置或获取释放键的回调。应支持递归与<see cref="LockCallback"/>配对使用。
        /// 默认值是<see cref="SingletonLocker.Exit(object)"/>。
        /// </summary>
        public Action<object> UnlockCallback { get; set; } = SingletonLocker.Exit;

        /// <summary>
        /// 确定当前线程是否保留指定键上的锁。
        /// 默认值是<see cref="SingletonLocker.IsEntered(object)"/>
        /// </summary>
        public Func<object, bool> IsEnteredCallback { get; set; } = SingletonLocker.IsEntered;

        #region IOptions 接口及相关

        public OwSchedulerOptions Value => this;
        #endregion IOptions 接口及相关
    }

    /// <summary>
    /// 一个用于处理长时间任务的任务计划器。
    /// 默认它仅用一个线程（不并行），因为假设的场景是一个服务器的IO线程。
    /// 每个任务会有一个唯一标识key，调用任务会首先锁定key,若不能锁定则会再下次扫描时调用任务。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class OwScheduler : IDisposable
    {
        #region 构造函数及相关

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="options"></param>
        public OwScheduler(IOptions<OwSchedulerOptions> options)
        {
            Options = options.Value;
            Initializer();
        }

        private void Initializer()
        {
            _Timer = new System.Threading.Timer(TimeCallback, null, Options.Frequency, Options.Frequency);
        }

        private OwSchedulerOptions _Options;
        /// <summary>
        /// 配置对象。
        /// </summary>
        public OwSchedulerOptions Options { get => _Options ??= new OwSchedulerOptions(); init => _Options = value; }

        #endregion 构造函数及相关

        #region 定时任务及相关

        /// <summary>
        /// 优先执行任务的列表。键是锁定项的键，使用此类可避免锁定。
        /// </summary>
        ConcurrentDictionary<object, object> _Plans = new ConcurrentDictionary<object, object>();

        /// <summary>
        /// 优先执行的任务的任务对象。
        /// </summary>
        Task _Task;

        System.Threading.Timer _Timer;
        /// <summary>
        /// 定时任务。
        /// </summary>
        /// <param name="state"></param>
        private void TimeCallback(object state)
        {
            foreach (var kvp in _Items)
            {
                using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, kvp.Key, TimeSpan.Zero);
                if (dw.IsEmpty) //若无法锁定
                {
                    Run(kvp.Key, false);
                    continue;
                }
                if (kvp.Value.Period == Timeout.InfiniteTimeSpan)   //若是无限周期的
                    continue;
                var now = DateTime.UtcNow;
                if (now - kvp.Value.LastUtc >= kvp.Value.Period)    //若周期时间已到
                {
                    try
                    {
                        RunCore(kvp.Key);
                    }
                    catch (Exception)
                    {
                        //TODO 处理异常
                    }
                }
                Thread.Yield();
            }
            SchedulerCallback();
        }

        /// <summary>
        /// 将优先计划的项全部执行。
        /// </summary>
        private void RunPlanFunc()
        {
            foreach (var kvp in _Plans) //遍历工作项
            {
                using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, kvp.Key, TimeSpan.Zero);
                if (dw.IsEmpty || !RunCore(kvp.Key)) //若无法锁定或执行不成功
                    continue;
                _Plans.TryRemove(kvp);
            }
        }

        /// <summary>
        /// 计划执行优先任务。不可重入，不能多线程并发。
        /// </summary>
        private void SchedulerCallback()
        {
            if (_Task is null || _Task.IsCompleted) //若没有任务或已经完成任务
            {
                _Task?.Dispose();
                _Task = Task.Run(RunPlanFunc);
            }
        }

        #endregion 定时任务及相关

        /// <summary>
        /// 是否已经完成所有定时任务。
        /// </summary>
        ManualResetEvent _Completed = new ManualResetEvent(false);
        /// <summary>
        /// 是否已经完成所有定时任务。
        /// </summary>
        public ManualResetEvent Completed { get => _Completed; }

        /// <summary>
        /// 任务项。
        /// </summary>
        ConcurrentDictionary<object, OwSchedulerEntry> _Items = new ConcurrentDictionary<object, OwSchedulerEntry>();

        /// <summary>
        /// 执行指定键值的任务。
        /// 非公有函数不会自动对键加锁，若需要则调用者需负责加/解锁。
        /// </summary>
        /// <param name="key"></param>
        /// <returns>同步执行时返回任务的返回值。异步执行时返回true。</returns>
        protected virtual bool RunCore(object key)
        {
            var now = DateTime.UtcNow;
            var entry = _Items.GetValueOrDefault(key);
            var b = entry.TaskCallback?.Invoke(key, entry.State) ?? true;
            if (b)
                while (entry.LastUtc < now)
                    entry.LastUtc += entry.Period;
            return b;
        }

        /// <summary>
        /// 执行指定键值的任务。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sync">true同步执行，false(默认)计划优先执行。</param>
        /// <returns>同步执行时返回任务的返回值。异步执行时返回true标识成功增加优先任务，false表示优先队列中有一个同样的项尚未执行。</returns>
        public bool Run(object key, bool sync = false)
        {
            if (sync) //若同步执行
            {
                using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Timeout.InfiniteTimeSpan);
                var b = RunCore(key);
                if (b)
                    _Plans.Remove(key, out _);
                return b;
            }
            else //若计划优先执行
            {
                var b = _Plans.TryAdd(key, null);
                if (b)
                    SchedulerCallback();
                return b;
            }
        }

        /// <summary>
        /// 增加一个任务项。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryAdd(object key, OwSchedulerEntry value)
        {
            value.IsValid();
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Timeout.InfiniteTimeSpan);
            return _Items.TryAdd(key, value);
        }

        /// <summary>
        /// 移除一个任务项。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool TryRemove(object key)
        {
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Timeout.InfiniteTimeSpan);
            bool result = _Items.Remove(key, out _);
            if (result)  //若成功移除任务
                _Plans.Remove(key, out _);
            return result;
        }

        #region IDisposable接口及相关

        private bool _DisposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!_DisposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Timer?.Dispose(new ManualResetEvent(false));
                _Timer = null;
                //_Task = null;
                _Items = null;
                _Plans = null;
                _Completed = null;
                _DisposedValue = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~OwScheduler()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口及相关
    }
}
