using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using OW.Game.Store;
using OW.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Base
{
    public class ThingManagerOptions : IOptions<ThingManagerOptions>
    {
        public ThingManagerOptions Value => this;

        /// <summary>
        /// Gets or sets the minimum length of time between successive scans for expired items.
        /// </summary>
        public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// 内部设置项。
    /// </summary>
    public class ThingManagerEntry
    {
        public ThingManagerEntry()
        {

        }

        /// <summary>
        /// 用于存储的数据库上下文。
        /// </summary>
        public DbContext Context { get; set; }

        /// <summary>
        /// 使用的缓存对象内部的配置键。
        /// </summary>
        public OwMemoryCache.OwMemoryCacheEntry Entry { get; set; }
    }

    /// <summary>
    /// <see cref="VirtualThing"/> 和 <see cref="OrphanedThing"/>类的管理服务类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class ThingManager : GameManagerBase<ThingManagerOptions, ThingManager>
    {
        #region 构造函数相关

        public ThingManager(IOptions<ThingManagerOptions> options, ILogger<ThingManager> logger, IServiceProvider service, TemplateManager templateManager) : base(options, logger)
        {
            Initializer();
            Service = service;
            _TemplateManager = templateManager;
        }

        /// <summary>
        /// 初始化函数。
        /// </summary>
        void Initializer()
        {
            Scheduler.TryAdd(_Key.ToString(), new OwSchedulerEntry()
            {
                Key = _Key.ToString(),
                Period = Options.ExpirationScanFrequency,
                TaskCallback = TimerFunc,
            });

        }

        #endregion 构造函数相关

        /// <summary>
        /// 票据到账号Key的映射。
        /// </summary>
        ConcurrentDictionary<Guid, string> _Token2Key = new ConcurrentDictionary<Guid, string>();

        /// <summary>
        /// 角色到账号Key的映射。
        /// </summary>
        ConcurrentDictionary<Guid, string> _CharId2Key = new ConcurrentDictionary<Guid, string>();

        /// <summary>
        /// 登录名到账号Key的映射。
        /// </summary>
        ConcurrentDictionary<string, string> _LoginNameId2Key = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 定时任务的Key。
        /// </summary>
        readonly Guid _Key = Guid.NewGuid();

        OwScheduler _Scheduler;
        /// <summary>
        /// 任务计划器。
        /// </summary>
        public OwScheduler Scheduler { get => _Scheduler ??= Service.GetRequiredService<OwScheduler>(); init => _Scheduler = value; }

        /// <summary>
        /// 键是缓存对象的键，值配置项。
        /// </summary>
        ConcurrentDictionary<object, ThingManagerEntry> _Entries = new ConcurrentDictionary<object, ThingManagerEntry>();

        /// <summary>
        /// 获取指定键的缓存配置项。
        /// 应首先锁定键再获取，否则行为未知。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal OwMemoryCache.OwMemoryCacheEntry GetEntry(object key) => Cache.GetEntry(key);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        bool TimerFunc(object taskKey, object state)
        {
            foreach (var key in Cache.Items.Keys)
            {
                using var dwKey = DisposeHelper.Create(Cache.TryEnter, Cache.Exit, key, TimeSpan.Zero);
                if (dwKey.IsEmpty)
                    continue;
                var entry = Cache.GetEntry(key);

            }
            return true;
        }

        public IServiceProvider Service { get; init; }

        private OwMemoryCache _Cache;
        /// <summary>
        /// 缓存对象。
        /// </summary>
        public OwMemoryCache Cache { get => _Cache ??= Service.GetRequiredService<OwMemoryCache>(); init => _Cache = value; }

        public void Add(object key, object value)
        {
        }

        /// <summary>
        /// 加载或获取缓存的对象。不会重新计算时间。
        /// </summary>
        /// <remarks><see cref="OrphanedThing"/> <see cref="VirtualThing"/>或其派生类的 RuntimeProperties["DbContext"] 中存放了使用的数据库上下文。</remarks>
        /// <typeparam name="TDbContext">加载数据使用的上下文对象。</typeparam>
        /// <typeparam name="TResult">返回对象的类型。</typeparam>
        /// <param name="key">缓存的键。</param>
        /// <param name="loadFunc">加载数据对象的过滤函数，必须返回唯一的对象。</param>
        /// <param name="initializer">初始换加载后的缓存配置数据。</param>
        /// <param name="dbContext">设置为上下文则使用此上下文加载，若是null则使用IDbContextFactory<TDbContext>服务创建一个并返回。</param>
        /// <returns></returns>
        public TResult GetOrLoadThing<TDbContext, TResult>(object key, Func<TResult, bool> loadFunc, Action<ThingManagerEntry> initializer, ref TDbContext dbContext)
             where TResult : class where TDbContext : DbContext
        {
            using var dwKey = DisposeHelper.Create(Cache.TryEnter, Cache.Exit, key, Cache.Options.DefaultLockTimeout);
            if (dwKey.IsEmpty)
                return null;
            var entry = Cache.GetEntry(key);
            if (entry is not null)  //若已加载
            {
                dbContext = _Entries.GetValueOrDefault(key).Context as TDbContext;
                return entry.Value as TResult;
            }
            dbContext ??= Service.GetRequiredService<IDbContextFactory<TDbContext>>().CreateDbContext();
            var result = dbContext.Set<TResult>().SingleOrDefault(loadFunc);
            using (entry = Cache.CreateEntry(key) as OwMemoryCache.OwMemoryCacheEntry)
            {
                var tme = new ThingManagerEntry
                {
                    Context = dbContext,
                    Entry = entry,
                };
                entry.RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    var cache = state as OwMemoryCache;
                    var entry = cache.GetEntry(key);
                    SaveCore(entry);
                }, Cache);
                entry.SetValue(result);
                _Entries.TryAdd(key, tme);
                initializer(tme);
            }
            return (TResult)entry.Value;
        }

        /// <summary>
        /// 存储指定项。
        /// </summary>
        /// <param name="entry"></param>
        void SaveCore(OwMemoryCache.OwMemoryCacheEntry entry)
        {
            var value = entry.Value;
            ConcurrentDictionary<string, object> dic;
            if (value is VirtualThing vt)
                dic = vt.RuntimeProperties;
            else if (value is OrphanedThing ot)
                dic = ot.RuntimeProperties;
            else
                return;
            var db = dic.GetValueOrDefault("DbContext") as DbContext;
            db.SaveChanges();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="key"></param>
        /// <param name="initializer"></param>
        /// <param name="dbContext">设置为上下文则使用此上下文加载，若是null则使用IDbContextFactory<TDbContext>服务创建一个并返回。</param>
        /// <returns></returns>
        public TResult GetOrCreate<TDbContext, TResult>(object key, Func<ThingManagerEntry, TResult> initializer, ref TDbContext dbContext)
            where TResult : class where TDbContext : DbContext
        {
            using var dwKey = DisposeHelper.Create(Cache.TryEnter, Cache.Exit, key, Cache.Options.DefaultLockTimeout);
            if (dwKey.IsEmpty)
                return null;
            var entry = Cache.GetEntry(key);
            if (entry is not null)
                return entry.Value as TResult;

            dbContext ??= Service.GetRequiredService<IDbContextFactory<TDbContext>>().CreateDbContext();
            using (entry = Cache.CreateEntry(key) as OwMemoryCache.OwMemoryCacheEntry)
            {
                var tme = new ThingManagerEntry() { Context = dbContext, Entry = entry };
                entry.RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    var cache = state as OwMemoryCache;
                    var entry = cache.GetEntry(key);
                    SaveCore(entry);
                }, Cache);
                entry.SetValue(initializer(tme));
            }
            return entry.Value as TResult;
        }

        /// <summary>
        /// 获取指定键的缓存对象。
        /// </summary>
        /// <param name="key"></param>
        /// <returns>指定键的缓存对象，若没有找到指定键可能是空引用。</returns>
        public object Get(object key)
        {
            return GetEntry(key)?.Value;
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public ThingManagerEntry CreateEntry<TDbContext>(object key) where TDbContext : DbContext
        {
            DbContext db = Service.GetRequiredService<IDbContextFactory<TDbContext>>().CreateDbContext();
            var result = new ThingManagerEntry()
            {
                Context = db,
                Entry = Cache.CreateEntry(key) as OwMemoryCache.OwMemoryCacheEntry,
            };

            return result;
        }

        #region IDisposable接口相关

        /// <summary>
        /// 通过检测<see cref="OwHelper.GetLastError"/>返回值是否为258(WAIT_TIMEOUT)决定是否抛出异常<seealso cref="TimeoutException"/>。
        /// </summary>
        /// <param name="msg"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfTimeout(string msg)
        {
            if (OwHelper.GetLastError() == 258)
                throw new TimeoutException(msg);
        }

        /// <summary>
        /// 根据<see cref="OwHelper.GetLastError"/>返回值判断是否抛出锁定键超时的异常。
        /// </summary>
        /// <param name="key"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfLockKeyTimeout(object key)
        {
            if (OwHelper.GetLastError() == 258)
                throw new TimeoutException($"锁定键时超时，键:{key}");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                if (disposing)
                {
                    //释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Entries = null;
                _Scheduler = null;
                _Cache = null;
                _Token2Key = null;
                _LoginNameId2Key = null;
                _CharId2Key = null;
            }
            base.Dispose(disposing);
        }

        #endregion IDisposable接口相关

        TemplateManager _TemplateManager;
        public GameEntityBase GetEntityBase(VirtualThing thing)
        {
            var tt = _TemplateManager.Id2FullView.GetValueOrDefault(thing.ExtraGuid, null);
            var type = TemplateManager.GetTypeFromTemplate(tt);
            return thing.GetJsonObject(type) as GameEntityBase;
        }

    }

    public static class ThingManagerExtensions
    {
    }
}
