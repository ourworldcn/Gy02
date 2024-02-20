using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Store;
using System.Collections.Concurrent;

namespace OW.GameDb
{
    public static class GY02LogginMigrateDbInitializer
    {
        public static void Initialize(GY02LogginContext context)
        {
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }

        }
    }

    /// <summary>
    /// 日志数据库上下文对象。
    /// </summary>
    /// <remarks>构造函数不能有除了DbContextOptions以外的参数，这是因为连接池时DbContext对象会被连接池保存，
    /// 这意味着DbContext对象是单例的，所以不能有其他注入的服务。
    /// 数据库连接字符串默认参数是pooling=true，如果改为pooling=false也会对启用数据库连接池造成影响。</remarks>
    public class GY02LogginContext : DbContext
    {
        public GY02LogginContext(DbContextOptions<GY02LogginContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<ActionRecord> ActionRecords { get; set; }

    }

    public class GameSqlLoggingManagerOptions : IOptions<GameSqlLoggingManagerOptions>
    {
        public GameSqlLoggingManagerOptions Value => this;

        public int MaxSaveCount { get; set; } = 100;

        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(10);
    }

    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameSqlLoggingManager
    {
        public GameSqlLoggingManager(IDbContextFactory<GY02LogginContext> context, IHostApplicationLifetime applicationLifetime, IOptions<GameSqlLoggingManagerOptions> options, ILogger<GameSqlLoggingManager> logging)
        {
            _Context = context.CreateDbContext();
            _ApplicationLifetime = applicationLifetime;

            _ApplicationLifetime.ApplicationStopping.Register(() => _Actions.CompleteAdding());
            _Task = Task.Factory.StartNew(OnWork, TaskCreationOptions.LongRunning);
            _Options = options.Value;
            _Logging = logging;
        }

        GY02LogginContext _Context;
        IHostApplicationLifetime _ApplicationLifetime;
        BlockingCollection<ActionRecord> _Actions = new BlockingCollection<ActionRecord>();
        Task _Task;
        GameSqlLoggingManagerOptions _Options;
        ILogger<GameSqlLoggingManager> _Logging;

        public void AddLogging(ActionRecord actionRecord)
        {
            _Actions.Add(actionRecord);
        }

        void OnWork()
        {
            CancellationTokenSource cts = new CancellationTokenSource((int)_Options.MaxDelay.TotalMilliseconds);
            var totalCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, _ApplicationLifetime.ApplicationStopping);
            try
            {
                while (true)
                {
                    totalCts.Token.WaitHandle.WaitOne();    //等到超期
                    try
                    {
                        while (_Actions.TryTake(out var item))
                            _Context.Add(item);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    using var tmp = cts;
                    cts = new CancellationTokenSource((int)_Options.MaxDelay.TotalMilliseconds);
                    totalCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, _ApplicationLifetime.ApplicationStopping);
                    try
                    {
                        _Context.SaveChanges();
                    }
                    catch (Exception err)
                    {
                        _Logging.LogError(err, "保存日志数据时出错。");
                    }
                    if (_Context.ChangeTracker.Entries().Count() > _Options.MaxSaveCount)
                    {
                        _Context.ChangeTracker.Clear();
                    }
                    if (_ApplicationLifetime.ApplicationStopping.IsCancellationRequested) break;
                }
            }
            catch (OperationCanceledException)
            { }
            catch (ObjectDisposedException)
            { }
        }
    }
}
