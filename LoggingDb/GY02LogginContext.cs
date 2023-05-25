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

    public class GY02LogginContext : DbContext
    {
        public GY02LogginContext(DbContextOptions<GY02LogginContext> options) : base(options)
        {
        }

        public GY02LogginContext()
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameActionRecord>().HasIndex(c => new { c.DateTimeUtc, c.ActionId }).IsUnique(false);
            modelBuilder.Entity<GameActionRecord>().HasIndex(c => new { c.ActionId, c.DateTimeUtc }).IsUnique(false);
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<GameActionRecord> ActionRecords { get; set; }

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
        public GameSqlLoggingManager(GY02LogginContext context, IHostApplicationLifetime applicationLifetime, IOptions<GameSqlLoggingManagerOptions> options, ILogger<GameSqlLoggingManager> logging)
        {
            _Context = context;
            _ApplicationLifetime = applicationLifetime;

            _ApplicationLifetime.ApplicationStopping.Register(() => _Actions.CompleteAdding());
            _Task = Task.Factory.StartNew(OnWork, TaskCreationOptions.LongRunning);
            _Options = options.Value;
            _Logging = logging;
        }

        GY02LogginContext _Context;
        IHostApplicationLifetime _ApplicationLifetime;
        BlockingCollection<GameActionRecord> _Actions = new BlockingCollection<GameActionRecord>();
        Task _Task;
        GameSqlLoggingManagerOptions _Options;
        ILogger<GameSqlLoggingManager> _Logging;

        public void AddLogging(GameActionRecord actionRecord)
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
