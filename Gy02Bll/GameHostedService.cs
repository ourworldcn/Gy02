using AutoMapper;
using GuangYuan.GY001.TemplateDb;
using GY02.Base;
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
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.GameDb;
using OW.Server;
using OW.SyncCommand;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

            _ApplicationLifetime.ApplicationStopped.Register(() =>
            {
                _Logger.LogInformation(new EventId(10001), "游戏虚拟世界服务正常下线。");
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
                Task.Run(InitializeAdmin);
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
            var sqlMemoryInMB = 4096;   //SQL服务器内存大小
            var mi = new MEMORYINFO(); mi.Initialize();
            if (Win32Methods.GlobalMemoryStatusEx(ref mi))
            {
                var tmp = (decimal)mi.ullTotalPhys / 1024 / 1024;
                sqlMemoryInMB = Math.Max(sqlMemoryInMB, (int)Math.Round(tmp / 3));
            }

            var fact = _Services.GetService<IDbContextFactory<GY02UserContext>>();
            using var db = fact.CreateDbContext();
            sqlSb.AppendLine(@$"EXEC sys.sp_configure N'show advanced options', N'1'  RECONFIGURE WITH OVERRIDE;" +
                $"EXEC sys.sp_configure N'max server memory (MB)', N'{sqlMemoryInMB}';" +
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

        /// <summary>
        /// 初始化管理员账号。
        /// </summary>
        void InitializeAdmin()
        {
            using var dw = DisposeHelper.Create(SingletonLocker.TryEnter, SingletonLocker.Exit, ProjectContent.AdminLoginName, Timeout.InfiniteTimeSpan);
            if (dw.IsEmpty) throw new InvalidOperationException("无法锁定管理员登录名。");

            var store = _Services.GetRequiredService<GameAccountStoreManager>();
            var fac = _Services.GetRequiredService<IDbContextFactory<GY02UserContext>>();
            using var db = fac.CreateDbContext();
            var tmp = db.VirtualThings.FirstOrDefault(c => c.ExtraGuid == ProjectContent.UserTId && c.ExtraString == ProjectContent.AdminLoginName);
            if (tmp is null)    //若需要初始化管理员账号
            {
                using var scope = _Services.CreateScope();
                CreateAccountCommand createUser = new CreateAccountCommand { LoginName = ProjectContent.AdminLoginName, Pwd = ProjectContent.AdminPwd };
                var handler = scope.ServiceProvider.GetRequiredService<SyncCommandManager>();
                handler.Handle(createUser);
                if (createUser.HasError) throw new InvalidOperationException("无法创建超管账号。");
                using var dwUser = DisposeHelper.Create(store.Lock, store.Unlock, createUser.User.Key, Timeout.InfiniteTimeSpan);
                if (dw.IsEmpty) throw new InvalidOperationException("无法锁定管理员对象。");
                var gc = createUser.User.CurrentChar;
                gc.Roles.Add(ProjectContent.SupperAdminRole);
                store.Save(createUser.User.Key);
            }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        [Conditional("DEBUG")]
        private void Test(string str = null)
        {
            var store = _Services.GetService<GameAccountStoreManager>();
            var mapper = _Services.GetService<IMapper>();
            var sw = Stopwatch.StartNew();

            #region 测试用代码
            try
            {
                var svc = _Services.GetRequiredService<T127Manager>();
                using HttpClient client = new HttpClient();
                var b = svc.GetOrderState("10001", "difcmdipfplohjejdkedbpng.AO-J1OwDekYaJx6G9yq8Ji1DSnFVr2PjSPqyvVm6AuvDx4I1E6t70KDE7KzOG8B452j1J2V4MgZrtoGDQMytZDVPlwB1CevFYxerrlgslpewcqbh8Cx1-60",
                    out var result);
            }
            #endregion 测试用代码
            catch (Exception)
            {
            }
            finally
            {
                sw.Stop();
                Debug.WriteLine($"测试用时:{sw.ElapsedMilliseconds:0.0}ms");
            }
        }

    }

    //private unsafe void Awake()
    //{
    //    byte[] sendByte = Encoding.ASCII.GetBytes("");//FORWARDERS

    //    fixed (byte* pointerToFirst = &sendByte[0])
    //    {
    //    }
    //}
}