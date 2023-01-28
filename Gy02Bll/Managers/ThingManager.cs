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
    /// <see cref="VirtualThing"/> 和 <see cref="OrphanedThing"/>类的管理服务类。
    /// </summary>
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

        void Initializer()
        {
            _Timer = new Timer(TimerFunc, null, Options.ExpirationScanFrequency, Options.ExpirationScanFrequency);
        }
        #endregion 构造函数相关

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
        /// <returns></returns>
        public TResult GetOrLoadThing<TDbContext, TResult>(object key, Func<TResult, bool> loadFunc, Action<OwMemoryCache.OwMemoryCacheEntry> initializer, TDbContext dbContext = null)
            where TResult : DbQuickFindBase, new() where TDbContext : DbContext
        {
            using var dwKey = DisposeHelper.Create(Cache.TryEnter, Cache.Exit, key, Cache.Options.DefaultLockTimeout);
            if (dwKey.IsEmpty)
                return null;
            var entry = Cache.GetEntry(key);
            if (entry is not null)  //若已加载
                return entry.Value as TResult;
            using (entry = Cache.CreateEntry(key) as OwMemoryCache.OwMemoryCacheEntry)
            {
                var db = Service.GetRequiredService<IDbContextFactory<TDbContext>>().CreateDbContext();
                var result = db.Set<TResult>().FirstOrDefault(loadFunc);
                //TODO 只能处理这两种类或其派生类
                if (result is OrphanedThing orphaned)
                    orphaned.RuntimeProperties["DbContext"] = db;
                else if (result is VirtualThing virtualThing)
                    virtualThing.RuntimeProperties["DbContext"] = db;
                entry.SetValue(result);
                entry.RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    var entry = Cache.GetEntry(key);
                    SaveCore(entry);
                }, null);
                initializer(entry);
            }
            return entry.Value as TResult;
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
        /// <returns></returns>
        public TResult GetOrCreate<TDbContext, TResult>(object key, Func<OwMemoryCache.OwMemoryCacheEntry, TResult> initializer)
            where TResult : DbQuickFindBase, new() where TDbContext : DbContext
        {
            using var dwKey = DisposeHelper.Create(Cache.TryEnter, Cache.Exit, key, Cache.Options.DefaultLockTimeout);
            if (dwKey.IsEmpty)
                return null;
            var entry = Cache.GetEntry(key);
            if (entry is not null)
                return entry.Value as TResult;
            using (entry = Cache.CreateEntry(key) as OwMemoryCache.OwMemoryCacheEntry)
            {
                initializer(entry);
                entry.RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    var entry = Cache.GetEntry(key);
                    SaveCore(entry);
                });
            }
            return entry.Value as TResult;
        }
    }

    public static class ThingManagerExtensions
    {

    }
}
