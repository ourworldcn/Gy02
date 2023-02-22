﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OW.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwDbBase
{
    public class DataObjectManagerOptions : IOptions<DataObjectManagerOptions>
    {
        #region 构造函数及相关

        public DataObjectManagerOptions()
        {
        }

        #endregion 构造函数及相关
        public DataObjectManagerOptions Value => this;

        /// <summary>
        /// 扫描间隔。
        /// </summary>
        /// <value>默认1分钟。</value>
        public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1.0);

        /// <summary>
        /// 当有一个对象加载到内存中时的回调。默认没有回调。
        /// </summary>
        public Action<object> LoadCallback { get; set; }

        /// <summary>
        /// 设置或获取锁定键的回调。应支持递归与<see cref="UnlockCallback"/>配对使用。
        /// 默认值是<see cref="Monitor.TryEnter(object, TimeSpan)"/>。
        /// </summary>
        public Func<object, TimeSpan, bool> LockCallback { get; set; } = Monitor.TryEnter;

        /// <summary>
        /// 设置或获取释放键的回调。应支持递归与<see cref="LockCallback"/>配对使用。
        /// 默认值是<see cref="Monitor.Exit(object)"/>。
        /// </summary>
        public Action<object> UnlockCallback { get; set; } = Monitor.Exit;

        /// <summary>
        /// 确定当前线程是否保留指定键上的锁。
        /// 默认值是<see cref="Monitor.IsEntered(object)"/>
        /// </summary>
        public Func<object, bool> IsEnteredCallback { get; set; } = Monitor.IsEntered;

        /// <summary>
        /// 默认的锁定超时时间。
        /// </summary>
        /// <value>默认值:3秒。</value>
        public TimeSpan DefaultLockTimeout { get; set; } = TimeSpan.FromSeconds(3);

    }

    public abstract class DataObjectEntry
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="cache">指定所属缓存对象，在调用<see cref="Dispose"/>时可以加入该对象。</param>
        protected DataObjectEntry(object key, OwMemoryCache cache)
        {
            Key = key;
            Cache = cache;
        }

        /// <summary>
        /// 所属的缓存对象。
        /// </summary>
        public OwMemoryCache Cache { get; set; }

        #region ICacheEntry接口相关

        object _Key;

        public object Key { get => _Key; set => _Key = value; }

        public virtual object Value { get; set; }

        public DateTimeOffset? AbsoluteExpiration { get; set; }

        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

        public TimeSpan? SlidingExpiration { get; set; }

        public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

        internal Lazy<List<PostEvictionCallbackRegistration>> _PostEvictionCallbacksLazyer = new Lazy<List<PostEvictionCallbackRegistration>>(true);
        /// <summary>
        /// 所有的函数调用完毕才会解锁键对象。
        /// </summary>
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => _PostEvictionCallbacksLazyer.Value;

        public CacheItemPriority Priority { get; set; }

        public long? Size { get; set; }

        #endregion ICacheEntry接口相关

        //internal Lazy<List<BeforeEvictionCallbackRegistration>> _BeforeEvictionCallbacksLazyer = new Lazy<List<BeforeEvictionCallbackRegistration>>(true);
        /// <summary>
        /// 获取或设置从缓存中即将逐出缓存项时将触发的回叫。
        /// 所有的函数调用完毕才会解锁键对象。
        /// 支持并发初始化，但返回集合本身不能支持并发。
        /// </summary>
        //public IList<BeforeEvictionCallbackRegistration> BeforeEvictionCallbacks => _BeforeEvictionCallbacksLazyer.Value;

        /// <summary>
        /// 最后一次使用的Utc时间。
        /// </summary>
        public DateTime LastUseUtc { get; internal set; } = DateTime.UtcNow;

        /// <summary>
        /// 获取此配置项是否超期。
        /// </summary>
        /// <param name="utcNow"></param>
        /// <returns></returns>
        public virtual bool IsExpired(DateTime utcNow)
        {
            if (SlidingExpiration.HasValue && utcNow - LastUseUtc >= SlidingExpiration)
                return true;
            if (AbsoluteExpiration.HasValue && utcNow >= AbsoluteExpiration)
                return true;
            return false;
        }

        /// <summary>
        /// 获取或设置用户的附加配置数据。
        /// </summary>
        public object State { get; set; }

    }

    /// <summary>
    /// EF实体类缓存管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class DataObjectManager : IDisposable
    {
        #region 构造函数及相关
        public DataObjectManager()
        {

        }

        public DataObjectManager(IOptions<DataObjectManagerOptions> options, ILogger<DataObjectManager> logger, OwServerMemoryCache cache,
            IEnumerable<IHostedService> ss, IServiceProvider service)
        {
            Options = options.Value;
            Logger = logger;
            _Cache = cache;
            _Service = service;
            _OwScheduler = ss.First(c => c is OwScheduler) as OwScheduler;
        }


        #endregion 构造函数及相关

        private DataObjectManagerOptions _Options;
        /// <summary>
        /// 
        /// </summary>
        public DataObjectManagerOptions Options { get => _Options; internal set => _Options = value; }

        private ILogger<DataObjectManager> _Logger;
        /// <summary>
        /// 
        /// </summary>
        public ILogger<DataObjectManager> Logger { get => _Logger; internal set => _Logger = value; }

        OwScheduler _OwScheduler;
        public OwScheduler Scheduler { get => _OwScheduler; }

        OwServerMemoryCache _Cache;
        public OwServerMemoryCache Cache { get => _Cache; }

        IServiceProvider _Service;

        /// <summary>
        /// 
        /// </summary>
        public IServiceProvider Service => _Service;


        public void TryAdd(object key, object value)
        {

        }

        public void TryRemove(object key) { }

        public void GetOrLoad<TContext, TSource>(object key, Func<TSource, bool> predicate)
            where TContext : DbContext where TSource : class
        {
            var db = _Service.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext();
            var value = db.Set<TSource>().First(predicate);
        }

        #region IDisposable接口及相关

        private bool _Disposed;

        protected bool Disposed => _Disposed;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Cache = null;
                _OwScheduler = null;
                _Logger = null;
                _Service = null;
                _Disposed = true;
            }
        }

        private void CheckDisposed()
        {
            if (_Disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        #endregion IDisposable接口及相关

    }
}
