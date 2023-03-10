using Gy02Bll;
using Gy02Bll.Managers;
using Microsoft.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;
using OW.Game.Manager;
using OW.Game.Store;
using OW.Server;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OW.Game
{
    public static class GameExtensions
    {
        public static IServiceCollection AddGameServices(this IServiceCollection services)
        {
            Assembly[] assemblies = new Assembly[] { typeof(GameUserBaseContext).Assembly, typeof(SyncCommandManager).Assembly, typeof(SqlDbFunctions).Assembly };
            HashSet<Assembly> hsAssm = new HashSet<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
            assemblies.ForEach(c => hsAssm.Add(c));

            services.AutoRegister(hsAssm);
            services.UseSyncCommand(hsAssm);

            services.TryAddSingleton<PasswordGenerator>(); //密码生成器
            services.TryAddSingleton<LoginNameGenerator>();    //登录名生成器
            services.TryAddSingleton<OwServerMemoryCache>();

            services.AddOwScheduler();
            services.TryAddSingleton<ISystemClock, OwSystemClock>();    //若没有时钟服务则增加时钟服务

            services.AddUdpServerManager();
            return services;
        }
    }
}
