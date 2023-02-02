using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OW.OW.Server
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
        public TimeSpan Period { get; set; }

        /// <summary>
        /// 上次执行的时间点。
        /// </summary>
        public DateTime LastUtc { get; internal set; }
    }

    public class OwSchedulerOptions : IOptions<OwSchedulerOptions>
    {
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
    public class OwScheduler
    {
        #region 构造函数及相关

        public OwScheduler()
        {
            Initializer();
        }

        public OwScheduler(IOptions<OwSchedulerOptions> options)
        {
            Options = options.Value;
            Initializer();
        }

        private void Initializer()
        {
            _Timer = new Timer(TimeCallback, null, Options.Frequency, Options.Frequency);
        }

        #endregion 构造函数及相关

        Timer _Timer;
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
                    Plan(kvp.Key);
                    continue;
                }
                if (kvp.Value.Period == Timeout.InfiniteTimeSpan)   //若是无限周期的
                    continue;
                if (DateTime.UtcNow - kvp.Value.LastUtc > kvp.Value.Period)
                {
                    RunCore(kvp.Key);
                    kvp.Value.LastUtc = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// 将优先计划的项全部执行。
        /// </summary>
        private void RunPlan()
        {
            DisposeHelper<object[]> dw;
            int count;
            Span<object> span;
            lock (_Plans)
            {
                count = _Plans.Count;
                var ary = ArrayPool<object>.Shared.Rent(count);
                dw = DisposeHelper.Create(c => ArrayPool<object>.Shared.Return(c), ary);
                span = ary.AsSpan(0, count);
            }
            using var s = dw;

        }

        public OwSchedulerOptions Options { get; init; }

        ConcurrentDictionary<object, OwSchedulerEntry> _Items = new ConcurrentDictionary<object, OwSchedulerEntry>();

        protected virtual bool RunCore(object key)
        {
            var entry = _Items.GetValueOrDefault(key);
            return entry.TaskCallback?.Invoke(key, entry.State) ?? true;
        }

        /// <summary>
        /// 增加一个任务项。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryAdd(object key, OwSchedulerEntry value)
        {
            using (var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Timeout.InfiniteTimeSpan))
                return _Items.TryAdd(key, value);
        }

        /// <summary>
        /// 移除一个任务项。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool TryRemove(object key)
        {
            using (var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Timeout.InfiniteTimeSpan))
                return _Items.Remove(key, out _);
        }

        /// <summary>
        /// 在该函数内立即执行指定的任务。
        /// </summary>
        public bool EnsureRun(object key)
        {
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Timeout.InfiniteTimeSpan);
            if (dw.IsEmpty)
                return false;
            return RunCore(key);
        }

        /// <summary>
        /// 优先执行任务的列表。
        /// </summary>
        HashSet<object> _Plans = new HashSet<object>();

        /// <summary>
        /// 无视定时优先执行安排指定任务。
        /// 不会立即执行而是在下次扫描时执行。
        /// </summary>
        public void Plan(object key)
        {
            lock (_Plans)
                _Plans.Add(key);
        }
    }
}
