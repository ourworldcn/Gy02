using GY02.Base;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;
using OW;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using OW.Server;
using OW.SyncCommand;
using System.Reflection;

namespace GY02.Base
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
            services.Replace(new ServiceDescriptor(typeof(ISystemClock), typeof(OwSystemClock), ServiceLifetime.Singleton));    //若没有时钟服务则增加时钟服务

            services.AddOwBackgroundScheduler();
            return services;
        }
    }

    public static class GameThingExtensions
    {

        /// <summary>
        /// 根据指定Id集合 获取实体数据。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tm"></param>
        /// <param name="gc"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetEntityAndTemplateFullView<T>(this GameTemplateManager tm, GameChar gc, IEnumerable<Guid> ids) where T : GameEntityBase
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
                    entity.SetTemplate(tfv);
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
        /// <returns>true成功，false失败，
        /// 此时使用<see cref="OwHelper.GetLastError"/>获取详细信息。</returns>
        public static bool SetTemplate(this GameTemplateManager manager, VirtualThing thing)
        {
            var tt = manager.GetFullViewFromId(thing.ExtraGuid);
            if (tt is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"对象{thing.Id} 的模板Id={thing.ExtraGuid},但找不到相应模板。");
                return false;
            }
            thing.SetTemplate(tt);
            return true;

        }
    }

}

namespace OW.Game
{
    public interface IGameCharCommand : ISyncCommand
    {
        public GameChar GameChar { get; set; }

    }

    public interface IGameCharHandler<T> : ISyncCommandHandler<T> where T : IGameCharCommand, IResultCommand
    {

        GameAccountStoreManager AccountStore { get; }

        /// <summary>
        /// 锁定角色。
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        public DisposeHelper<string> LockGameChar(T command)
        {
            var key = command.GameChar?.GetUser()?.Key;
            if (key == null)
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = $"无法找到用来锁定角色的Key。";
                return DisposeHelper.Empty<string>();
            }
            if (!AccountStore.Lock(key))   //若锁定失败
            {
                command.FillErrorFromWorld();
                return DisposeHelper.Empty<string>();
            }
            var dw = DisposeHelper.Create(AccountStore.Unlock, key);
            if (dw.IsEmpty) //若锁定失败
            {
                AccountStore.Unlock(key);
                command.FillErrorFromWorld();
                return DisposeHelper.Empty<string>();
            }
            return dw;
        }

        public string GetKey(T command)
        {
            var key = command.GameChar?.GetUser()?.Key;
            if (key == null)
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = $"无法找到用来锁定角色的Key。";
                return default;
            }
            return key;
        }
    }

}
