using Microsoft.Extensions.DependencyInjection;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OW.Game
{
    public static class GameExtensions
    {
        public static IServiceCollection AddGameServices(this IServiceCollection services)
        {
            Assembly[] assemblies = new Assembly[] { typeof(GameUserBaseContext).Assembly, typeof(GameCommandManager).Assembly, typeof(SqlDbFunctions).Assembly };
            HashSet<Assembly> hsAssm = new HashSet<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
            assemblies.ForEach(c => hsAssm.Add(c));

            services.AutoRegister(hsAssm);
            
            return services;
        }
    }
}
