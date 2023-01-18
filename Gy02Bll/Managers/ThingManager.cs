using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OW.Game.Store;
using System;
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
        }

        /// <summary>
        /// 内部构造函数。
        /// </summary>
        private ThingManager()
        {
        }

        #endregion 构造函数相关

        public IServiceProvider Service { get; init; }

        public ThingManagerOptions Options { get; init; }

        public DataObjectManager DataObjectManager { get; init; }

        public void GetOrLoadVirtualThing<TDbContext, TResult>(object key, Guid dbKey, Action<TResult> initializer) where TResult : VirtualThing, new()
        {
            var result = DataObjectManager.GetOrLoad<TResult>(key, (item, entry) =>
            {
                return new object();
            });
        }

        public void GetOrLoadOrphanedThing<TDbContext, TResult>(object key, Func<TResult, bool> loadFunc, Action<DataObjectItemEntry, OwMemoryCache.OwMemoryCacheEntry> initializer) where TResult : OrphanedThing, new() where TDbContext : DbContext
        {
            var result = DataObjectManager.GetOrLoad<TResult>(key, (item, entry) =>
            {
                var fact = Service.GetRequiredService<IDbContextFactory<TDbContext>>();
                var db = fact.CreateDbContext();
                var entity = db.Set<TResult>().FirstOrDefault(loadFunc);
                entity.RuntimeProperties["DbContext"] = db;
                initializer(item, entry);
                return entity;
            });
        }
    }

    public static class ThingManagerExtensions
    {

    }
}
