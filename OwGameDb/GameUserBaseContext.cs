using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OW.Data;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Store
{
    /// <summary>
    /// 仅统计当前有多少命令在执行。大致可以反应数据库造成的IO压力。
    /// </summary>
    public class OwGameCommandInterceptor : DbCommandInterceptor
    {
        public static volatile int QueryExecutingCount;

        public static volatile int ReaderExecutingCount;

        public static volatile int ScalarExecutingCount;

        public static int ExecutingCount => QueryExecutingCount + ReaderExecutingCount + ScalarExecutingCount;

        /// <summary>
        /// 每当并发的操作数减少时会发出信号。
        /// </summary>
        public static AutoResetEvent ExecutingCountChanged = new(false);

        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwGameCommandInterceptor()
        {
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            Interlocked.Increment(ref QueryExecutingCount);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            Interlocked.Decrement(ref QueryExecutingCount);
            ExecutingCountChanged.Set();
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Interlocked.Increment(ref ReaderExecutingCount);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            Interlocked.Decrement(ref ReaderExecutingCount);
            ExecutingCountChanged.Set();
            return base.ReaderExecuted(command, eventData, result);
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            Interlocked.Increment(ref ScalarExecutingCount);
            return base.ScalarExecuting(command, eventData, result);
        }

        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            Interlocked.Decrement(ref ScalarExecutingCount);
            ExecutingCountChanged.Set();
            return base.ScalarExecuted(command, eventData, result);
        }

    }

    /// <summary>
    /// 游戏的玩家数据存储的数据上下文。
    /// </summary>
    public class GameUserBaseContext : DbContext
    {
        protected GameUserBaseContext()
        {
        }

        public GameUserBaseContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(new OwGameCommandInterceptor());
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //自定义代码
            //注册函数
            SqlDbFunctions.Register(modelBuilder);

            //调用基类方法。
            base.OnModelCreating(modelBuilder);
        }

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
                }
            }
        }
        #endregion 保存前

    }
}
