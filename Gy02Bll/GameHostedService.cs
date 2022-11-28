using GuangYuan.GY001.TemplateDb;
using Gy02Bll.Commands;
using Gy02Bll.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OW.Game.Caching;
using OW.Game.Manager;
using OW.Game.Managers;
using OW.Game.Store;
using OwDbBase;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

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
            Task.Run(Preloading);
            return result;
        }

        private void SetDbConfig()
        {
            using var scope = _Services.CreateScope();
            var svc = scope.ServiceProvider;
            var sqlSb = AutoClearPool<StringBuilder>.Shared.Get();
            using var dwSb = DisposeHelper.Create(c => AutoClearPool<StringBuilder>.Shared.Return(c), sqlSb);
            #region 设置sql server使用内存，避免sql server 贪婪使用内存导致内存过大

            var db = svc.GetService<GY02UserContext>();
            sqlSb.AppendLine(@$"EXEC sys.sp_configure N'show advanced options', N'1'  RECONFIGURE WITH OVERRIDE;" +
                "EXEC sys.sp_configure N'max server memory (MB)', N'4096';" +
                "RECONFIGURE WITH OVERRIDE;" +
                "EXEC sys.sp_configure N'show advanced options', N'0'  RECONFIGURE WITH OVERRIDE;");
            #endregion

            //压缩
            var table = db?.Model.FindEntityType(typeof(VirtualThing))?.GetTableMappings();

            var tn = db?.Model.FindEntityType(typeof(VirtualThing))?.GetTableName();
            sqlSb.AppendLine($"ALTER TABLE {tn} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE);");
            sqlSb.AppendLine($"ALTER INDEX IX_VirtualThings_ExtraGuid_ExtraDecimal_ExtraString ON {tn} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE);");
            sqlSb.AppendLine($"ALTER INDEX IX_VirtualThings_ExtraGuid_ExtraString_ExtraDecimal ON {tn} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE);");

            try
            {
                db?.Database.ExecuteSqlRaw(sqlSb.ToString());
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

                var context = services.GetRequiredService<GY02UserContext>();
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
            object i2 = 5.ToString();
            var sw = Stopwatch.StartNew();
            try
            {
                var str = JsonSerializer.Serialize<Gy02TemplateJO>(new Gy02TemplateJO { });

                var ss = JsonSerializer.Deserialize<Gy02TemplateJO>(str);
            }
            finally
            {
                sw.Stop();
                Debug.WriteLine($"测试用时:{sw.ElapsedMilliseconds:0.0}ms");
            }
        }

        /// <summary>
        /// 预先初始化一些必须的服务。
        /// </summary>
        private void Preloading()
        {
            var cache = _Services.GetService<TemplateManager>();
        }
    }
}