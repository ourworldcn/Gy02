using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Game.Store
{
    public class ServerConfigItem
    {
        /// <summary>
        /// Key的名字。
        /// </summary>
        [MaxLength(64)]
        [Key]
        public string Name { get; set; }

        /// <summary>
        /// 内容。
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 最后修改的日期。使用游戏世界时间。
        /// </summary>
        public DateTime LastModifyUtc { get; set; } = OwHelper.WorldNow;
    }

    public static class MigrateDbInitializer
    {
        public static void Initialize(GY02UserContext context)
        {
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }

        }
    }

    /// <summary>
    /// 游戏的玩家数据库上下文。
    /// </summary>
    /// <remarks>保存时会对跟踪的数据中支持<see cref="IBeforeSave"/>接口的对象调用<see cref="IBeforeSave.PrepareSaving(DbContext)"/></remarks>
    public class GY02UserContext : GameUserBaseContext
    {
        public GY02UserContext([NotNull] DbContextOptions options) : base(options)
        {

        }

        protected GY02UserContext()
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //树状节点对象
            modelBuilder.Entity<VirtualThing>().HasIndex(c => new { c.ExtraGuid, c.ExtraString, c.ExtraDecimal }).IsUnique(false).IncludeProperties(c => c.ParentId);
            modelBuilder.Entity<VirtualThing>().HasIndex(c => new { c.ExtraGuid, c.ExtraDecimal, c.ExtraString }).IsUnique(false).IncludeProperties(c => c.ParentId);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<OrphanedThing> OrphanedThings { get; set; }

        /// <summary>
        /// 包含游戏世界内所有事物对象的表。
        /// </summary>
        public DbSet<VirtualThing> VirtualThings { get; set; }

        /// <summary>
        /// 服务器的全局配置项。
        /// </summary>
        public DbSet<ServerConfigItem> ServerConfig { get; set; }

    }
}
