﻿using GuangYuan.GY001.TemplateDb;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.TemplateDb;
using GY02.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OW.DDD;
using OW.Game;
using OW.Game.Conditional;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Managers;
using OW.Game.Store;
using OW.GameDb;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GY02
{
    /// <summary>
    /// 游戏世界的主服务。该服务退出表示游戏世界不再存在。
    /// </summary>
    public class GameHostedService : BackgroundService
    {
        public GameHostedService(IServiceProvider services, IHostApplicationLifetime applicationLifetime, ILogger<GameHostedService> logger)
        {
            _Services = services;
            _ApplicationLifetime = applicationLifetime;
            _Logger = logger;

            _ApplicationLifetime.ApplicationStopping.Register(() =>
            {
                _Logger.LogInformation($"检测到游戏服务器正常下线。");
            });
        }

        readonly IServiceProvider _Services;

        IHostApplicationLifetime _ApplicationLifetime;
        ILogger<GameHostedService> _Logger;

        /// <summary>
        /// 使用的服务容器。
        /// </summary>
        public IServiceProvider Services { get => _Services; }

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
                logger?.LogInformation("游戏虚拟世界服务成功上线。");
                using var scope = _Services.CreateScope();
                var service = scope.ServiceProvider;
                var mailManager = service.GetService<GameMailManager>();
                Task.Run(mailManager.ClearMail);
            }, _Services, cancellationToken);

            Test();
            return result;
        }

        /// <summary>
        /// 配置数据库。
        /// </summary>
        private void SetDbConfig()
        {
            using var scope = _Services.CreateScope();
            var svc = scope.ServiceProvider;
            var sqlSb = AutoClearPool<StringBuilder>.Shared.Get();
            using var dwSb = DisposeHelper.Create(c => AutoClearPool<StringBuilder>.Shared.Return(c), sqlSb);
            #region 设置sql server使用内存，避免sql server 贪婪使用内存导致内存过大

            var fact = _Services.GetService<IDbContextFactory<GY02UserContext>>();
            using var db = fact.CreateDbContext();
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
                var loggingContext = services.GetRequiredService<GY02LogginContext>();
                GY02LogginMigrateDbInitializer.Initialize(loggingContext);
                logger.LogTrace($"日志数据库已正常升级。");

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
                logger.LogError(err, "升级数据库出现错误。");
            }
        }

        public static Guid PingGuid = Guid.Parse("{D99A07D0-DF3E-43F7-8060-4C7140905A29}");

        [Conditional("DEBUG")]
        private void Test()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                #region 测试用代码

                var store = _Services.GetService<GameAccountStoreManager>();
                if (!store.LoadOrGetUser("string201", "string", out var user))
                    throw new InvalidOperationException { };
                var gc = user.CurrentChar;
                var entityManager = _Services.GetService<GameEntityManager>();
                var entities = entityManager.GetAllEntity(gc);
                var slot = entities.FirstOrDefault(c => c.TemplateId == Guid.Parse("9599B400-0BFD-498E-93DC-F44FF303B1B3"));  //巡逻用主线副本最高记录占位符
                if (slot.Count == 0) slot.Count++;
                slot = entities.FirstOrDefault(c => c.TemplateId == Guid.Parse("62D3A545-7604-46BF-9837-95E286660BC8"));  //巡逻时间占位符
                var i = slot.Count;
                #endregion 测试用代码
            }
            finally
            {
                sw.Stop();
                Debug.WriteLine($"测试用时:{sw.ElapsedMilliseconds:0.0}ms");
            }
        }

    }


}