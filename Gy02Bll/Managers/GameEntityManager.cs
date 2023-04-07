using Gy02.Publisher;
using Gy02Bll.Base;
using Gy02Bll.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Managers
{
    public class GameEntityManagerOptions : IOptions<GameEntityManagerOptions>
    {
        public GameEntityManagerOptions Value => this;
    }

    /// <summary>
    /// 实体对象的操作服务。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameEntityManager : GameManagerBase<GameEntityManagerOptions, GameEntityManager>
    {
        public GameEntityManager(IOptions<GameEntityManagerOptions> options, ILogger<GameEntityManager> logger, TemplateManager templateManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
        }

        TemplateManager _TemplateManager;

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

        /// <summary>
        /// 指定材料是否符合指定条件的要求。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="conditions"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMatch(GameEntity entity, GameThingPrecondition conditions) =>
            conditions.Any(c => IsMatch(entity, c));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public bool IsMatch(GameEntity entity, GameThingPreconditionItem condition)
        {
            VirtualThing thing = (VirtualThing)entity.Thing;
            TemplateStringFullView fullView = _TemplateManager.Id2FullView[thing.ExtraGuid];

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public bool IsPartialMatch(GameEntity entity, GameThingPreconditionItem condition)
        {
            
            return true;
        }

        #endregion 计算寻找物品的匹配

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
            var fullView = _TemplateManager.Id2FullView.GetValueOrDefault(entity.TemplateId);

            if (fullView?.LvUpTId is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"对象(Id={entity.Id})没有升级模板数据。");
                return result;
            }
            var tt = _TemplateManager.Id2FullView.GetValueOrDefault(fullView.LvUpTId.Value);
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

        /// <summary>
        /// 规范化物品，使之数量符合堆叠上限要求。
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public IEnumerable<GameEntity> Normalize(IEnumerable<GameEntity> src)
        {
            var result = new List<GameEntity>();
            src.SafeForEach(c => _TemplateManager.SetTemplate((VirtualThing)c.Thing));
            foreach (var item in src)
            {
                var tt = _TemplateManager.Id2FullView.GetValueOrDefault(item.TemplateId);
                if (tt is null)
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    OwHelper.SetLastErrorMessage($"物品{item.Id},没有有效模板TId={item.TemplateId}");
                    return null;
                }
                if (tt.Stk == 1)   //若是不可堆叠物
                {
                    var oldCount = item.Count;
                    if (Math.Abs(item.Count) > 1)    //若需要规范化
                    {
                        item.Count = Math.Sign(oldCount);
                        result.Add(item);
                        for (int i = Convert.ToInt32(Math.Abs(item.Count) - 1); i > 0 - 1; i--)
                        {

                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 修改虚拟物的数量。不进行参数校验的修改数量属性。并根据需要返回更改数据。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="count">数量的增量。</param>
        /// <param name="changes"></param>
        public void Modify(GameEntity entity, decimal count, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var template = _TemplateManager.Id2FullView.GetValueOrDefault(entity.TemplateId);
            if (template is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到Id={entity.TemplateId}的模板。");
                return;
            }
            var oldCount = entity.Count;
            entity.Count += count;
            changes?.Add(new GamePropertyChangeItem<object>()
            {
                Object = entity,
                PropertyName = "Count",
                DateTimeUtc = DateTime.UtcNow,
                HasOldValue = true,
                OldValue = oldCount,
                HasNewValue = true,
                NewValue = entity.Count,
            });
            if (entity.Count == 0 && !template.Count0Reserved) //若需要删除
            {
                var thing = entity.Thing as VirtualThing;
                var parent = thing.Parent;
                thing.Parent.Children.Remove(thing); thing.Parent = null; thing.ParentId = null;
                changes?.CollectionRemove(thing, parent);
                var db = (thing.GetRoot() as VirtualThing)?.RuntimeProperties.GetValueOrDefault(nameof(DbContext)) as DbContext;
                db?.Remove(thing);
            }
        }

        /// <summary>
        /// 获取指定虚拟物是否可以和指定容器中的虚拟物合并。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="container"></param>
        /// <param name="dest"></param>
        /// <returns>true指定容器中存在可合并的虚拟物，false没有可合并的虚拟物或出错，此时用<see cref="OwHelper.GetLastError"/>确定是否有错。</returns>
        public bool IsMerge(GameEntity entity, GameEntity container, out GameEntity dest)
        {
            var tmp = ((VirtualThing)container.Thing).Children.FirstOrDefault(c => c.ExtraGuid == entity.TemplateId);  //可能的合成物
            if (tmp is null) goto noMerge;    //若不能合并
            var tt = _TemplateManager.Id2FullView.GetValueOrDefault(tmp.ExtraGuid);
            if (tt is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                dest = null;
                return false;
            }
            if (tt.Stk == 1)   //若不可堆叠
                goto noMerge;
            else if (tt.Stk != -1)   //若不可无限堆叠
            {
                var entity2 = (GameEntity)_TemplateManager.GetEntityBase(tmp, out _);
                if (entity.Count + entity2.Count > tt.Stk) goto noMerge;    //若不可合并
                else
                {
                    dest = _TemplateManager.GetEntityBase(tmp, out _) as GameEntity;
                    return true;
                }
            }
            else //无限堆叠
            {
                dest = _TemplateManager.GetEntityBase(tmp, out _) as GameEntity;
                return true;
            }
        noMerge:
            dest = null;
            return false;
        }
    }
}
