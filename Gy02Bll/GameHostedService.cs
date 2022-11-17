using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using OW.Game.Store;
using OwDbBase;
using System.Diagnostics;

namespace Gy02Bll
{
    public class GameHostedService : BackgroundService
    {


        public GameHostedService(IServiceProvider services)
        {
            _Services = services;
        }

        public IServiceProvider _Services { get; set; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _Services.CreateScope();
            var service = scope.ServiceProvider;
            CreateDb(service);
            var result = Task.Factory.StartNew(c =>
            {
                //Thread thread = new Thread(() => CreateNewUserAndChar())
                //{
                //    IsBackground = true,
                //    Priority = ThreadPriority.Lowest,
                //};
                //#if DEBUG
                //                thread.Start();
                //#else
                //                thread.Start();
                //#endif
                //                Task.Run(CreateGameManager);    //强制初始化所有服务以加速
                Task.Run(SetDbConfig);  //设置数据库配置项
                var logger = _Services.GetService<ILogger<GameHostedService>>();
                logger?.LogTrace("游戏虚拟世界服务成功上线。");
            }, _Services, cancellationToken);

            Test();
            return result;
        }

        private void SetDbConfig()
        {
            #region 设置sql server使用内存，避免sql server 贪婪使用内存导致内存过大
            using var scope = _Services.CreateScope();
            var svc = scope.ServiceProvider;

            var db = svc.GetService<GameUserContext>();
            var sql = @$"EXEC sys.sp_configure N'show advanced options', N'1'  RECONFIGURE WITH OVERRIDE;" +
                "EXEC sys.sp_configure N'max server memory (MB)', N'4096';" +
                "RECONFIGURE WITH OVERRIDE;" +
                "EXEC sys.sp_configure N'show advanced options', N'0'  RECONFIGURE WITH OVERRIDE;";
            try
            {
                db?.Database.ExecuteSqlRaw(sql);
            }
            catch (Exception)
            {
            }
            #endregion

            try
            {
                var tn = db?.Model.FindEntityType(typeof(VirtualThing))?.GetTableName();
                sql = "ALTER TABLE [dbo].[GameItems] REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = ROW);" +
                    "ALTER INDEX IX_GameItems_TemplateId_ExtraString_ExtraDecimal ON [dbo].[GameItems] REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE)";   //按行压缩

                //TODO 压缩索引
                var indexNames = new string[] { "IX_VirtualThings_ExtraGuid_ExtraString_ExtraDecimal", "IX_VirtualThings_ExtraGuid_ExtraDecimal_ExtraString" };
                var alterIndex = "ALTER INDEX {0} ON [dbo].[GameItems] REBUILD PARTITION = ALL WITH(DATA_COMPRESSION = PAGE); ";

                db?.Database.ExecuteSqlRaw(sql);
                tn = db?.Model.FindEntityType(typeof(VirtualThing))?.GetTableName();
                if (tn != null)
                {
                    sql = $"ALTER TABLE {tn} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = ROW);";
                    db?.Database.ExecuteSqlRaw(sql);
                }
            }
            catch (Exception err)
            {
                Trace.WriteLine(err.Message);
            }
#if !DEBUG  //若正式运行版本

#endif
        }

        private void CreateDb(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<GameHostedService>>();
            try
            {
                var tContext = services.GetRequiredService<GY02TemplateContext>();
                TemplateMigrateDbInitializer.Initialize(tContext);
                logger.LogTrace($"模板数据库已正常升级。");

                var context = services.GetRequiredService<GameUserContext>();
                MigrateDbInitializer.Initialize(context);
                logger.LogTrace("用户数据库已正常升级。");

                //var loggingDb = services.GetService<GameLoggingDbContext>();
                //if (loggingDb != null)
                //{
                //    GameLoggingMigrateDbInitializer.Initialize(loggingDb);
                //    logger.LogTrace("日志数据库已正常升级。");
                //}
            }
            catch (Exception err)
            {
                logger.LogError(err, $"升级数据库出现错误——{err.Message}");
            }
        }

        [Conditional("DEBUG")]
        private void Test()
        {
            var ss = _Services.GetService<AutoClearPool<List<int>>>();
            var svc = _Services.GetService<DataObjectManager>();
        }
    }
}