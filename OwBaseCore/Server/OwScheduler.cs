using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
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
        /// 上次执行的时间点。null表示尚未运行过。
        /// </summary>
        public DateTime? LastUtc { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void IsValid()
        {
            if (Period == TimeSpan.Zero)
                throw new InvalidOperationException();
        }

        /// <summary>
        /// 是否到期。
        /// </summary>
        /// <param name="now"></param>
        /// <returns>true已经到期，false未到期。</returns>
        public bool IsExpired(DateTime now)
        {
            if (Period == Timeout.InfiniteTimeSpan)  //若仅运行一次
                return LastUtc is null;
            if (LastUtc is null)    //若尚未运行
                return true;
            return OwHelper.ComputeTimeout(LastUtc.Value, now) >= Period;
        }

        /// <summary>
        /// 增加最后运行时间。
        /// </summary>
        /// <param name="now">指定当前时间。</param>
        public void SetExpired(DateTime now)
        {
            if (LastUtc is null)    //若尚未运行过任务
            {
                LastUtc = now;
                return;
            }
            else
                while (LastUtc.Value < now)   //若时间未超过指定时间
                    LastUtc += Period;
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
        /// <value>默认值：1分钟。</value>
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
    public class OwScheduler : BackgroundService
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

        #region BackgroundService及相关

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //注册终止函数。
            stoppingToken.Register(c =>
            {
                try
                {
                    ExecuteTask?.Wait(); //等待后台任务结束
                }
                catch (Exception) { }

                return;
            }, null);
            //后台任务
            return Task.Factory.StartNew(stoppingTokenObj =>
            {
                CancellationToken ctStop = (CancellationToken)stoppingTokenObj;

                while (!stoppingToken.IsCancellationRequested)
                {
                    var now = DateTime.UtcNow;  //此轮工作的开始时间
                    foreach (var kvp in _Items)
                    {
                        using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, kvp.Key, TimeSpan.Zero);
                        if (dw.IsEmpty) //若无法锁定
                            continue;
                        if (!kvp.Value.IsExpired(now))   //若无需运行
                            continue;
                        try
                        {
                            RunCore(kvp.Key);
                        }
                        catch (Exception)
                        {//TODO 处理异常
                        }
                        Thread.Yield();
                    }
                    //等待
                    var ts = OwHelper.ComputeTimeout(now, Options.Frequency);
                    var task = Task.Run(() =>
                    {
                        using var dw = DisposeHelper.Create(Monitor.TryEnter, Monitor.Exit, _Plans, ts);
                        if (!dw.IsEmpty)
                        {
                            ts = OwHelper.ComputeTimeout(DateTime.UtcNow, now + Options.Frequency);
                            Monitor.Wait(_Plans, ts);
                        }
                    });
                    try
                    {
                        task.Wait((int)ts.TotalMilliseconds, ctStop);
                    }
                    catch (OperationCanceledException) { break; }
                }
            }, stoppingToken, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 运行所有任务。
        /// </summary>
        /// <param name="now"></param>
        void RunAll(DateTime now)
        {
            foreach (var kvp in _Items)
            {
                using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, kvp.Key, TimeSpan.Zero);
                if (dw.IsEmpty) //若无法锁定
                {
                    Run(kvp.Key);   //安排优先再次运行
                    continue;
                }
                if (!kvp.Value.IsExpired(now))   //若无需运行
                    continue;
                try
                {
                    if(!RunCore(kvp.Key))
                        Run(kvp.Key);   //安排优先再次运行
                }
                catch (Exception)
                {
                    Run(kvp.Key);   //安排优先再次运行
                }
                Thread.Yield();
            }
        }

        #endregion BackgroundService及相关

        #region IDisposable接口及相关

        private bool _Disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                //_Task = null;
                _Items = null;
                _Plans = null;
                _Disposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~OwScheduler()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        //public void Dispose()
        //{
        //    // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //    Dispose(disposing: true);
        //    GC.SuppressFinalize(this);
        //}

        #endregion IDisposable接口及相关

    }

    namespace App.QueueService
    {
        public interface IBackgroundTaskQueue
        {
            ValueTask QueueBackgroundWorkItemAsync(
                Func<CancellationToken, ValueTask> workItem);

            ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
                CancellationToken cancellationToken);
        }
    }
}
