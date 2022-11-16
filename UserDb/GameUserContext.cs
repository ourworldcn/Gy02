using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ObjectPool;
using OwGameDb.User;
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
    public static class MigrateDbInitializer
    {
        public static void Initialize(GameUserContext context)
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
    public class GameUserContext : GameUserBaseContext
    {
        public GameUserContext([NotNull] DbContextOptions options) : base(options)
        {

        }

        protected GameUserContext()
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //树状节点对象
            modelBuilder.Entity<VirtualThing>().HasIndex(c => new { c.ExtraGuid, c.ExtraString, c.ExtraDecimal }).IsUnique(false).IncludeProperties(c => c.ParentId);
            modelBuilder.Entity<VirtualThing>().HasIndex(c => new { c.ExtraGuid, c.ExtraDecimal, c.ExtraString }).IsUnique(false).IncludeProperties(c => c.ParentId);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<GameUserDo> GameUsers { get; set; }

        /// <summary>
        /// 包含游戏世界内所有事物对象的表。
        /// </summary>
        public DbSet<VirtualThing> VirtualThings { get; set; }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
