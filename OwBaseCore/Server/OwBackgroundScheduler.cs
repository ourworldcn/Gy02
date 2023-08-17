using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Server
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class OwBackgroundSchedulerOptions : IOptions<OwBackgroundSchedulerOptions>
    {
        public OwBackgroundSchedulerOptions Value => this;
    }

    /// <summary>
    /// <see cref="OwBackgroundScheduler"/> 内部配置条目。
    /// </summary>
    class OwBackgroundSchedulerEntry
    {
        public object Key { get; set; }

        public Func<object, bool> Func { get; set; }

        public object State { get; set; }

    }

    /// <summary>
    /// 用于执行后台任务的服务。通常这些任务，优先级低，耗时多，且可能短时间内有多个key相同的任务排入，实际仅执行一次即可。
    /// </summary>
    public class OwBackgroundScheduler : OwServiceBase<OwBackgroundSchedulerOptions, OwBackgroundScheduler>
    {
        #region 构造函数及相关

        public OwBackgroundScheduler(IOptions<OwBackgroundSchedulerOptions> options, ILogger<OwBackgroundScheduler> logger, IHostApplicationLifetime lifetime) : base(options, logger)
        {
            _Lifetime = lifetime;
            Initialize();
        }

        private void Initialize()
        {
            _Thread = new Thread(WorkFunc)
            {
                IsBackground = false,
                Name = "低优先级任务。",
                Priority = ThreadPriority.Lowest
            };
            _Thread.Start();
        }

        #endregion 构造函数及相关

        IHostApplicationLifetime _Lifetime;

        ConcurrentDictionary<object, OwBackgroundSchedulerEntry> _Enties = new ConcurrentDictionary<object, OwBackgroundSchedulerEntry>();

        Thread _Thread;

        public bool TryAdd(object key, Func<object, bool> task, object state)
        {
            if (_Lifetime.ApplicationStopped.IsCancellationRequested) throw new InvalidOperationException("已经终止操作。");
            _Enties.AddOrUpdate(key, c => new OwBackgroundSchedulerEntry() { Key = key, Func = task, State = state },
                (key, ov) => new OwBackgroundSchedulerEntry() { Key = key, Func = task, State = state });
            return true;
        }

        /// <summary>
        /// 确保已经加入的任务正常执行完毕。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout"></param>
        /// <returns>true成功完成任务，false锁定超时。</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool EnsureDone(object key, TimeSpan timeout)
        {
            if (_Lifetime.ApplicationStopped.IsCancellationRequested) throw new InvalidOperationException("已经终止操作。");
            using (var dw = DisposeHelper.Create(SingletonLocker.TryEnter, SingletonLocker.Exit, key, timeout))
            {
                if (dw.IsEmpty) return false;   //若无法锁定
                if (!_Enties.TryRemove(key, out var entry)) return true;    //若因某种原因已经并发处理了
                try
                {
                    if (!entry.Func(entry.State)) //若失败
                    {
                        _Enties.TryAdd(entry.Key, entry);
                        return false;
                    }
                }
                catch (Exception excp)
                {
                    _Enties.TryAdd(entry.Key, entry);
                    Logger.LogWarning(excp, "运行后台任务时引发异常。");
                    return false;
                }
            }
            return true;
        }

        void WorkFunc()
        {
            List<OwBackgroundSchedulerEntry> list = new List<OwBackgroundSchedulerEntry>();
            while (!_Lifetime.ApplicationStopped.IsCancellationRequested)
            {
                ScanAndDo();
                if (_Lifetime.ApplicationStopped.WaitHandle.WaitOne(1))
                    break;
            }
            Thread.CurrentThread.Priority = ThreadPriority.Normal;  //恢复正常优先级
            while (!_Enties.IsEmpty)
                ScanAndDo(true);
        }

        /// <summary>
        /// 扫描任务并执行。
        /// </summary>
        /// <param name="ignoreFailed">忽略返回错误的工作函数。</param>
        void ScanAndDo(bool ignoreFailed = false)
        {
            List<OwBackgroundSchedulerEntry> list = new List<OwBackgroundSchedulerEntry>();
            foreach (var item in _Enties)
            {
                using (var dw = DisposeHelper.Create(SingletonLocker.TryEnter, SingletonLocker.Exit, item.Key, TimeSpan.Zero))
                {
                    if (dw.IsEmpty) continue;   //若无法锁定
                    if (!_Enties.TryRemove(item.Key, out var entry)) continue;    //若因某种原因已经并发处理了
                    try
                    {
                        if (!entry.Func(entry.State))
                            if (!ignoreFailed)
                                list.Add(entry);
                    }
                    catch (Exception err)
                    {
                        Logger.LogWarning(err, "运行后台任务时引发异常。");
                        if (!ignoreFailed) list.Add(entry);
                    }
                }
                Thread.Yield(); //让出CPU给更高优先级的任务
            }
            list.ForEach(c => _Enties.TryAdd(c.Key, c));
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    //释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Thread = null;
                base.Dispose(disposing);
            }
        }
    }

    public static class OwBackgroundSchedulerExtensions
    {
        public static IServiceCollection AddOwBackgroundScheduler(this IServiceCollection services)
        {
            return services.AddSingleton<OwBackgroundScheduler>();
        }
    }
}
