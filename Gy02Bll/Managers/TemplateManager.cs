using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY02.Store;
using Gy02.Publisher;
using Gy02Bll.Commands;
using Gy02Bll.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Game.Managers
{
    /// <summary>
    /// 
    /// </summary>
    public class RawTemplateOptions : Collection<RawTemplate>
    {
        public RawTemplateOptions() : base()
        {

        }

    }

    public class TemplateManagerOptions : IOptions<TemplateManagerOptions>
    {
        public TemplateManagerOptions() { }

        public TemplateManagerOptions Value => this;
    }
    /// <summary>
    /// 
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class TemplateManager : GameManagerBase<TemplateManagerOptions, TemplateManager>
    {
        #region 静态成员

        static ConcurrentDictionary<Guid, Type> _TypeGuid2Type;
        /// <summary>
        /// 键是模板中TypeGuid属性，值是改模板创建的对象的实体类型。
        /// </summary>
        public static ConcurrentDictionary<Guid, Type> TypeGuid2Type
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _TypeGuid2Type, () =>
                {
                    var coll = AppDomain.CurrentDomain.GetAssemblies().SelectMany(c => c.GetTypes()).Where(c => !c.IsAbstract && c.IsAssignableTo(typeof(OwGameEntityBase)));
                    var tmp = new ConcurrentDictionary<Guid, Type>(coll.ToDictionary(c => c.GUID));
                    return tmp;
                });
            }
        }

        /// <summary>
        /// 获取模板的生成类的类型。
        /// </summary>
        /// <param name="fullView"></param>
        /// <returns>如果没有找到可能返回null。</returns>
        public static Type GetTypeFromTemplate(TemplateStringFullView fullView)
        {
            var result = TypeGuid2Type.GetValueOrDefault(fullView.TypeGuid);    //获取实例类型
            if (result is not null && fullView.SubTypeGuid is not null) //若是泛型类
            {
                var sub = TypeGuid2Type.GetValueOrDefault(fullView.SubTypeGuid.Value);
                result = result.MakeGenericType(sub);
            }
            return result;
        }

        #endregion 静态成员

        /// <summary>
        /// 获取模板的生成类的类型。
        /// </summary>
        /// <param name="tid"></param>
        /// <returns></returns>
        public Type GetTypeFromTId(Guid tid)
        {
            var tt = GetRawTemplateFromId(tid);
            var fv = tt?.GetJsonObject<TemplateStringFullView>();
            if (fv is null)
                return null;
            return GetTypeFromTemplate(fv);
        }

        public TemplateManager(IOptions<TemplateManagerOptions> options, GY02TemplateContext dbContext, ILogger<TemplateManager> logger, IHostApplicationLifetime lifetime, IOptionsMonitor<RawTemplateOptions> rawTemplateOptions)
            : base(options, logger)
        {
            DbContext = dbContext;
            logger.LogDebug("上线:模板管理器。");
            _Lifetime = lifetime;
            _RawTemplateOptions = rawTemplateOptions;
            _RawTemplateOptionsChangedMonitor = _RawTemplateOptions.OnChange(RawTemplateOptionsChanged);
            Initialize();
        }

        private void RawTemplateOptionsChanged(RawTemplateOptions arg1, string arg2)
        {

        }

        IDisposable _RawTemplateOptionsChangedMonitor;
        IOptionsMonitor<RawTemplateOptions> _RawTemplateOptions;
        IHostApplicationLifetime _Lifetime;
        Task _InitTask;
        private void Initialize()
        {
            //    var ary = DbContext.ThingTemplates.ToArray();
            //    _Id2Template = new ConcurrentDictionary<Guid, GY02ThingTemplate>(ary.ToDictionary(c => c.Id));

            _Lifetime.ApplicationStarted.Register(() =>
            {
            });
            _InitTask = Task.Run(() =>
            {
                try
                {
                    _Id2RawTemplate = new ConcurrentDictionary<Guid, RawTemplate>(_RawTemplateOptions.CurrentValue.ToDictionary(c => c.Id));
                    _Id2FullView = new ConcurrentDictionary<Guid, TemplateStringFullView>(_Id2RawTemplate.Select(c => c.Value.GetJsonObject<TemplateStringFullView>()).ToDictionary(c => c.TemplateId));
                }
                catch (Exception excp)
                {
                    Logger.LogWarning(excp, "读取模板数据出现错误。");
                    throw;
                }

            });

        }

        /// <summary>
        /// 
        /// </summary>
        public GY02TemplateContext DbContext { get; set; }

        ConcurrentDictionary<Guid, RawTemplate> _Id2RawTemplate;
        public ConcurrentDictionary<Guid, RawTemplate> Id2RawTemplate
        {
            get
            {
                _InitTask.Wait();
                return _Id2RawTemplate;
            }
        }

        ConcurrentDictionary<Guid, TemplateStringFullView> _Id2FullView;
        /// <summary>
        /// 所有模板全量视图。
        /// </summary>
        public ConcurrentDictionary<Guid, TemplateStringFullView> Id2FullView
        {
            get
            {
                _InitTask.Wait();
                return _Id2FullView;
            }
        }


        /// <summary>
        /// 获取指定Id的原始模板。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public RawTemplate GetRawTemplateFromId(Guid id)
        {
            return Id2RawTemplate.GetValueOrDefault(id);
        }

        /// <summary>
        /// 获取虚拟物的实体。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="type">返回值的实际类型。</param>
        /// <returns>返回的是<see cref="OwGameEntityBase"/>的派生对象。如果没找到则可能返回null。</returns>
        public OwGameEntityBase GetEntityBase(VirtualThing thing, out Type type)
        {
            var tt = Id2FullView.GetValueOrDefault(thing.ExtraGuid, null);
            if (tt is null) goto fail;
            type = GetTypeFromTemplate(tt);
            return thing.GetJsonObject(type) as OwGameEntityBase;
        fail:
            type = null;
            return null;
        }

        /// <summary>
        /// 获取指定虚拟物的模板和实体。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="entity">实际的实体类型。</param>
        /// <param name="fullView"></param>
        /// <returns></returns>
        public bool GetEntityAndTemplate(VirtualThing thing, out OwGameEntityBase entity, out TemplateStringFullView fullView)
        {
            fullView = Id2FullView.GetValueOrDefault(thing.ExtraGuid, null);
            if (fullView is null) goto error;
            var type = GetTypeFromTemplate(fullView);
            if (type is null) goto error;
            entity = thing.GetJsonObject(type) as OwGameEntityBase;
            return entity is not null;
        error:
            entity = null;
            return false;
        }

        /// <summary>
        /// 获取指定物品的升级所需材料及数量列表。
        /// </summary>
        /// <param name="entity">要升级的物品。</param>
        /// <param name="alls">搜索物品的集合。</param>
        /// <returns>升级所需物及数量列表。返回null表示出错了 <seealso cref="OwHelper.GetLastError"/>。
        /// 如果返回了找到的条目<see cref="ValueTuple{GameEntity,Decimal}.Item2"/> 对于消耗的物品是负数，可能包含0.。</returns>
        public List<(GameEntity, decimal)> GetCost(GameEntity entity, IEnumerable<GameEntity> alls)
        {
            List<(GameEntity, decimal)> result = null;
            var fullView = Id2FullView.GetValueOrDefault(entity.TemplateId);

            if (fullView?.LvUpTId is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"对象(Id={entity.Id})没有升级模板数据。");
                return result;
            }
            var tt = Id2FullView.GetValueOrDefault(fullView.LvUpTId.Value);
            if (tt?.LvUpData is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"对象(Id={entity.Id})没有升级模板数据。");
                return result;
            }
            var lv = Convert.ToInt32(entity.Level);
            var coll = tt.LvUpData.Select(c =>
            {
                decimal count = 0;
                var tmp = alls.FirstOrDefault(item => IsMatch(entity, c, item, out count));
                return (entity: tmp, count);
            }).ToArray();
            if (coll.Count() != tt.LvUpData.Count)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);
                OwHelper.SetLastErrorMessage($"对象(Id={entity.Id})升级材料不全。");
                return result;
            }
            var errItem = coll.GroupBy(c => c.entity.Id).Where(c => c.Count() > 1).FirstOrDefault();
            if (errItem is not null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"物品(Id={errItem.First().entity.Id})同时符合两个或更多条件。");
                return result;
            }

            return result = coll.ToList();
        }

        #region 计算寻找物品的匹配

        /// <summary>
        /// 指定材料是否符合指定条件的要求。
        /// </summary>
        /// <param name="main">要升级的物品</param>
        /// <param name="cost">要求的条件。</param>
        /// <param name="entity">材料的实体。</param>
        /// <param name="count">实际耗费的数量</param>
        /// <returns>true表示指定实体符合指定条件，否则返回false。</returns>
        public bool IsMatch(GameEntity main, CostInfo cost, GameEntity entity, out decimal count)
        {
            if (IsMatch(entity, cost.Conditional))
            {
                var lv = Convert.ToInt32(main.Level);
                if (cost.Conditional is not null && cost.Counts.Count > lv)
                {
                    var tmp = cost.Counts[lv];  //耗费的数量
                    if (tmp <= entity.Count)
                    {
                        count = -Math.Abs(tmp);
                        return true;
                    }
                }
            }
            count = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMatch(GameEntity entity, GameThingPrecondition conditions) =>
            conditions.Any(c => IsMatch(entity, c));

        public bool IsMatch(GameEntity entity, GameThingPreconditionItem condition)
        {
            VirtualThing thing = (VirtualThing)entity.Thing;
            TemplateStringFullView fullView = Id2FullView[thing.ExtraGuid];

            if (!condition.TId.HasValue && condition.TId.Value != thing.ExtraGuid)
                return false;
            if (condition.Genus is not null && condition.Genus.Count > 0 && condition.Genus.Intersect(fullView.Genus).Count() != condition.Genus.Count)
                return false;
            if (condition.ParentTId.HasValue && condition.ParentTId.Value != thing.Parent?.ExtraGuid)
                return false;
            if (condition.MinCount.HasValue && condition.MinCount.Value > entity.Count)
                return false;
            return true;
        }

        #endregion 计算寻找物品的匹配

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            _RawTemplateOptionsChangedMonitor?.Dispose();
            _RawTemplateOptionsChangedMonitor = null;
            base.Dispose(disposing);
        }
        #endregion IDisposable
    }

    public static class TemplateManagerExtensions
    {
        public static IServiceCollection AddTemplateManager(this IServiceCollection services)
        {
            return services;
        }
    }
}
