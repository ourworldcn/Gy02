using Microsoft.EntityFrameworkCore;
using OwGameDb.User;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Store
{
    public class GameUserDbContext : DbContext
    {
        #region 保存前

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            PrepareSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
            Task.Run(() =>
            {
                PrepareSaving();
                return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            });

        /// <summary>
        /// 在保存被调用。
        /// </summary>
        private void PrepareSaving()
        {
            var coll = ChangeTracker.Entries().Select(c => c.Entity).OfType<IBeforeSave>().Where(c => !c.SuppressSave).ToList();
            foreach (var item in coll)
            {
                try
                {
                    item.PrepareSaving(this);
                }
                catch (Exception err)
                {
                    Debug.WriteLine($"预保存时发生错误——{err.Message}");
                    throw;
                }
            }
        }
        #endregion 保存前

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //自定义代码
            //注册函数
            SqlDbFunctions.Register(modelBuilder);

            //树状节点对象
            modelBuilder.Entity<VirtualThing>().HasIndex(c => new { c.ExtraGuid, c.ExtraString, c.ExtraDecimal }).IsUnique(false).IncludeProperties(c => c.ParentId);
            modelBuilder.Entity<VirtualThing>().HasIndex(c => new { c.ExtraGuid, c.ExtraDecimal, c.ExtraString }).IsUnique(false).IncludeProperties(c => c.ParentId);

            //调用基类方法。
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<GameUserDo> GameUsers { get; set; }

        public DbSet<VirtualThing> VirtualThings { get; set; }
    }
}
