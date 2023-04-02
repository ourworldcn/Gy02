using Gy02.Publisher;
using Gy02Bll;
using Gy02Bll.Managers;
using Gy02Bll.Templates;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.Server;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    public static class GameThingExtensions
    {
        public static VirtualThing GetGameCharThing(this VirtualThing thing)
        {
            for (var tmp = thing; tmp is not null; tmp = tmp.Parent)
            {
                if (tmp.ExtraGuid == ProjectContent.CharTId)
                    return tmp;
            }
            return null;
        }

        public static VirtualThing GetThing(this OwGameEntityBase entity) => entity.Thing as VirtualThing;

        public static TemplateStringFullView GetTemplateStringFullView(this OwGameEntityBase entity) =>
            entity.GetThing()?.RuntimeProperties.GetValueOrDefault("Template") as TemplateStringFullView;

        public static void SetTemplateStringFullView(this OwGameEntityBase entity, TemplateStringFullView tfv) => entity.GetThing().RuntimeProperties["Template"] = tfv;

        /// <summary>
        /// 根据指定Id集合 获取实体数据。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tm"></param>
        /// <param name="gc"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetEntityAndTemplateFullView<T>(this TemplateManager tm, GameChar gc, IEnumerable<Guid> ids) where T : OwGameEntityBase
        {
            var id2Thing = gc.GetThing().GetAllChildren().ToDictionary(c => c.Id);
            var result = new List<T>();

            foreach (var item in ids)
            {
                var thing = id2Thing.GetValueOrDefault(item);
                if (thing is null)
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    OwHelper.SetLastErrorMessage($"指定Id={item}不是角色的子对象。");
                    return null;
                }
                if (!tm.GetEntityAndTemplate(thing, out var entity, out var tfv)) return null;
                if (entity is T tmp)
                {
                    entity.SetTemplateStringFullView(tfv);
                    result.Add(tmp);
                }
                else
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    OwHelper.SetLastErrorMessage($"对象={entity.Id}不是{typeof(T)}类。");
                    return null;
                }
            }
            return result;
        }

        /// <summary>
        /// 给虚拟物填写模板属性。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="thing"></param>
        /// <returns></returns>
        public static bool SetTemplate(this TemplateManager manager, VirtualThing thing)
        {
            var tt = manager.Id2FullView.GetValueOrDefault(thing.ExtraGuid);
            if (tt is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"对象{thing.Id} 的模板Id={thing.ExtraGuid},但找不到相应模板。");
                return false;
            }
            thing.RuntimeProperties["Template"] = tt;
            return true;

        }

        /// <summary>
        /// 设置指定属性的值。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <param name="pi"></param>
        /// <param name="difference">差值，正数增加，负数减少。</param>
        /// <param name="changes"></param>
        public static void SetPropertyValue(this GameEntity entity, PropertyDescriptor pi, decimal difference, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var oldVal = Convert.ToDecimal(pi.GetValue(entity));   //旧属性值
            var newVal = oldVal + difference; //新属性值
            pi.SetValue(entity, newVal);    //设置属性值
            changes?.Add(new GamePropertyChangeItem<object>
            {
                Object = entity,
                PropertyName = pi.Name,
                OldValue = oldVal,
                HasOldValue = true,
                NewValue = newVal,
                HasNewValue = true,
            });
        }

    }

    public static class MyClass
    {
        public static void IsMatch(this GameThingPrecondition preconditionItems)
        {

        }
    }
}
