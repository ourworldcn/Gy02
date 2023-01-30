using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Managers
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
    public class ThingManager
    {
        #region 构造函数相关

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        public ThingManager([NotNull] IServiceProvider service, [NotNull] ThingManagerOptions options) : this()
        {
            Service = service;
            Options = options;
            DataObjectManager = new DataObjectManager(new DataObjectManagerOptions(), Service);
            Initializer();
        }

        /// <summary>
        /// 内部构造函数。
        /// </summary>
        private ThingManager()
        {
        }

        /// <summary>
        /// 初始化函数。
        /// </summary>
        void Initializer()
        {
            _Timer = new Timer(TimerFunc, null, Options.ExpirationScanFrequency, Options.ExpirationScanFrequency);
        }

        #endregion 构造函数相关

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

        Timer _Timer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        void TimerFunc(object state)
        {
            foreach (var key in Cache.Items.Keys)
            {
                using (var dwKey = DisposeHelper.Create(Cache.TryEnter, Cache.Exit, key, TimeSpan.Zero))
                {
                    if (dwKey.IsEmpty)
                        continue;
                    var entry = Cache.GetEntry(key);
                }

            }
        }

        public IServiceProvider Service { get; init; }

        public ThingManagerOptions Options { get; init; }

        public DataObjectManager DataObjectManager { get; init; }

        public OwMemoryCache Cache { get; init; } = new OwMemoryCache(new OwMemoryCacheOptions());

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
                return entry.Value as TResult;
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
    }

    public static class ThingManagerExtensions
    {
    }
}
