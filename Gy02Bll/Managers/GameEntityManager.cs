using Gy02.Publisher;
using Gy02Bll.Base;
using Gy02Bll.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.DDD;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
        public GameEntityManager(IOptions<GameEntityManagerOptions> options, ILogger<GameEntityManager> logger, TemplateManager templateManager, VirtualThingManager virtualThingManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
            _VirtualThingManager = virtualThingManager;
        }

        TemplateManager _TemplateManager;
        VirtualThingManager _VirtualThingManager;

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

            if (condition.TId.HasValue && condition.TId.Value != thing.ExtraGuid)
                return false;
            if (condition.Genus is not null && condition.Genus.Count > 0 && condition.Genus.Intersect(fullView.Genus).Count() != condition.Genus.Count)
                return false;
            if (condition.ParentTId.HasValue && condition.ParentTId.Value != thing.Parent?.ExtraGuid)
                return false;
            if (condition.MinCount.HasValue && condition.MinCount.Value > entity.Count)
                return false;
            return true;
        }

        public bool IsMatch(TemplateStringFullView entity, GameThingPreconditionItem condition)
        {
            if (condition.TId.HasValue && condition.TId.Value != entity.TemplateId)
                return false;
            if (condition.Genus is not null && condition.Genus.Count > 0 && condition.Genus.Intersect(entity.Genus).Count() != condition.Genus.Count)
                return false;
            //if (condition.ParentTId.HasValue && condition.ParentTId.Value != thing.Parent?.ExtraGuid)
            //return false;
            //if (condition.MinCount.HasValue && condition.MinCount.Value > entity.Count)
            //    return false;
            return true;
        }


        /// <summary>
        /// 忽略计数的情况下判断是否符合条件。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public bool IsPartialMatch(GameEntity entity, GameThingPreconditionItem condition)
        {
            var thing = entity.GetThing();
            TemplateStringFullView fullView = _TemplateManager.Id2FullView[entity.TemplateId];
            if (condition.TId.HasValue && condition.TId.Value != entity.TemplateId)
                return false;
            if (condition.Genus is not null && condition.Genus.Count > 0 && condition.Genus.Intersect(fullView.Genus).Count() != condition.Genus.Count)
                return false;
            if (condition.ParentTId.HasValue && condition.ParentTId.Value != thing.Parent?.ExtraGuid)
                return false;
            return true;
        }

        //public bool IsNoStk(GameThingPreconditionItem condition)
        //{
        //    if (!condition.TId.HasValue)
        //        return false;
        //    TemplateStringFullView fullView = _TemplateManager.Id2FullView[entity.TemplateId];

        //}
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
            if (coll.Count(c => c.entity is not null) != tt.LvUpData.Count)
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
        /// <param name="count">数量的增量，即正数为增加，负数为减少。</param>
        /// <param name="changes"></param>
        /// <returns>true成功，否则返回false,此时用<see cref="OwHelper.GetLastError"/>获取详细信息。</returns>
        public bool Modify(GameEntity entity, decimal count, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var template = _TemplateManager.GetFullViewFromId(entity.TemplateId);
            if (template is null) return false;
            var oldCount = entity.Count;
            entity.Count += count;
            changes?.Add(new GamePropertyChangeItem<object>()
            {
                Object = entity,
                PropertyName = nameof(entity.Count),
                DateTimeUtc = DateTime.UtcNow,
                HasOldValue = true,
                OldValue = oldCount,
                HasNewValue = true,
                NewValue = entity.Count,
            });
            if (entity.Count == 0 && !template.Count0Reserved) //若需要删除
            {
                Delete(entity, changes);
            }
            return true;
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
            var tt = _TemplateManager.GetFullViewFromId(tmp.ExtraGuid);
            if (tt is null) goto noMerge;   //若无法找到模板
            if (!tt.IsStk())   //若不可堆叠
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

        #region 基础功能

        /// <summary>
        /// 获取虚拟对象中寄宿的游戏实体对象。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns>寄宿其中的游戏实体对象，如果不是则返回null，此时用<see cref="OwHelper.GetLastError"/>确定是否有错。</returns>
        public GameEntity GetEntity(VirtualThing thing)
        {
            var result = _TemplateManager.GetEntityBase(thing, out _) as GameEntity;
            if (result is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"指定虚拟对象没有寄宿的对象不是{typeof(GameEntity)}类型,Id={thing.Id}");
            }
            return result;
        }

        /// <summary>
        /// 获取容器的虚拟对象。
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>容器的虚拟对象,返回null表示没有父或出错，此时用<see cref="OwHelper.GetLastError"/>获取详细信息。0表示没有父对象。</returns>
        public VirtualThing GetParentThing(GameEntity entity)
        {
            var thing = entity.GetThing();
            if (thing is null) return null;
            OwHelper.SetLastError(ErrorCodes.NO_ERROR);
            return thing.Parent;
        }

        /// <summary>
        /// 获取指定实体的父实体。
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>指定实体的容器实体，如果发生错误则返回null，此时用<see cref="OwHelper.GetLastError"/>确定是否有错,可能返回无错误(0)这说明指定实体本旧没有容器。</returns>
        public GameEntity GetParent(GameEntity entity)
        {
            var parentThing = GetParentThing(entity);
            if (parentThing is null) return null;
            return GetEntity(parentThing);
        }

        /// <summary>
        /// 获取模板。
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>模板，如果返回null,此时用<see cref="OwHelper.GetLastError"/>获取详细信息。</returns>
        public TemplateStringFullView GetTemplate(GameEntity entity) =>
            _TemplateManager.GetFullViewFromId(entity.TemplateId);

        /// <summary>
        /// 获取实体的默认容器。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="gc"></param>
        /// <returns></returns>
        public GameEntity GetDefaultContainer(GameEntity entity, GameChar gc)
        {
            if (entity is null)
                throw new ArgumentNullException(nameof(entity));
            if (gc is null)
                throw new ArgumentNullException(nameof(gc));

            var tt = _TemplateManager.GetFullViewFromId(entity.TemplateId); //获取模板
            if (tt is null) return null;
            var ptid = tt.ParentTId;

            var all = gc.GetAllChildren();
            if (all is null) return null;

            var parent = all.FirstOrDefault(c => c.ExtraGuid == ptid);
            if (parent is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"无法获取指定角色的所有子对象，CharId={gc.Id}");
                return null;
            }
            return GetEntity(parent);
        }

        #endregion 基础功能

        #region 创建实体相关功能

        /// <summary>
        /// 创建实体。
        /// </summary>
        /// <param name="idAndCount">对不可堆叠物品，会创建多个对象，每个对象数量是1。</param>
        /// <returns>创建实体的集合，任何错误导致返回null，此时用<see cref="OwHelper.GetLastError"/>获取详细信息。</returns>
        public List<GameEntity> Create(IEnumerable<(Guid, decimal)> idAndCount)
        {
            var result = new List<GameEntity> { };
            foreach (var item in idAndCount)
            {
                var tt = _TemplateManager.GetFullViewFromId(item.Item1);
                if (tt is null) return null;
                if (tt.IsStk())  //可堆叠物
                {
                    var tmp = _VirtualThingManager.Create(tt);
                    if (tmp is null) return null;
                    var entity = GetEntity(tmp);
                    if (entity is null) return null;
                    entity.Count = item.Item2;
                    result.Add(entity);
                }
                else //不可堆叠物
                {
                    var count = (int)item.Item2;
                    if (count != item.Item2)
                    {
                        OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                        OwHelper.SetLastErrorMessage($"创建不可堆叠物的数量必须是整数。TId={item.Item1}");
                        return null;
                    }
                    var tmp = _VirtualThingManager.Create(tt.TemplateId, count);
                    if (tmp is null) return null;
                    foreach (var thing in tmp)
                    {
                        var tmpEntity = GetEntity(thing);
                        if (tmpEntity is null) return null;
                        tmpEntity.Count = 1;
                        result.Add(tmpEntity);
                    }
                }
            }
            return result;
        }

        #endregion 创建实体相关功能

        #region 删除实体相关功能

        /// <summary>
        /// 从数据库中删除指定实体极其宿主对象。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="changes"></param>
        /// <returns></returns>
        public bool Delete(GameEntity entity, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var thing = entity.GetThing();
            if (thing is null) return false;
            var parent = GetParent(entity);
            var db = (thing.GetRoot() as VirtualThing)?.RuntimeProperties.GetValueOrDefault(nameof(DbContext)) as DbContext;
            if (db is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定实体宿主对象的数据库上下文，Id={entity.Id}");
                return false;
            }
            var result = _VirtualThingManager.Delete(thing, db);
            if (result)
            {
                changes?.CollectionRemove(entity, parent);
            }
            return result;
        }

        #endregion 删除实体相关功能

        #region 改变实体相关功能

        /// <summary>
        /// 把指定实体从其父容器中移除，但不删除实体。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="changes"></param>
        /// <returns>true一处成功，false指定实体没有父容器或移除失败，此时用<see cref="OwHelper.GetLastError"/>获取详细信息，0表示指定实体本就没有父容器。</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool RemoveFromContainer(GameEntity entity, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var parent = GetParent(entity);
            if (parent is null) //若出错或没有父容器
            {
                return false;
            }
            else
            {
                var parentThing = parent.GetThing();
                if (parentThing is null) return false;
                var thing = entity.GetThing();   //不应失败
                if (!parentThing.Children.Remove(thing)) //若异常错误
                    throw new InvalidOperationException { };
                thing.Parent = null;
                thing.ParentId = null;
                changes?.CollectionRemove(entity, parent);
                return true;
            }
        }

        /// <summary>
        /// 移动物品到它模板指定的默认容器中。
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="gameChar"></param>
        /// <param name="changes"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Move(IEnumerable<GameEntity> entities, GameChar gameChar, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            //TODO 要测试是否能移动
            foreach (var entity in entities)
            {
                var b = Move(entity, gameChar, changes);
                if (!b)
                    throw new InvalidOperationException { };
            }
        }

        /// <summary>
        /// 移动物品到它模板指定的默认容器中。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="gameChar"></param>
        /// <param name="changes"></param>
        /// <returns></returns>
        public bool Move(GameEntity entity, GameChar gameChar, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var container = GetDefaultContainer(entity, gameChar);
            if (container == null) return false;
            return Move(entity, container, changes);
        }

        public void Move(IEnumerable<GameEntity> entities, GameEntity container, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            //TODO 需要判断是否可以完整移入
            foreach (var entity in entities)
                Move(entity, container, changes);
        }

        public bool Move(GameEntity entity, GameEntity container, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var tt = _TemplateManager.GetFullViewFromId(entity.TemplateId);
            if (tt is null) return false;
            if (tt.IsStk())  //若是可堆叠物
            {
                if (!RemoveFromContainer(entity, changes) && OwHelper.GetLastError() != ErrorCodes.NO_ERROR) return false;
                if (IsMerge(entity, container, out var dest))  //若找到可合并物
                {
                    Modify(dest, entity.Count, changes);
                }
                else //没有可合并物
                {
                    var parentThing = container.GetThing();
                    if (parentThing is null) return false;
                    var thing = entity.GetThing();
                    if (thing is null) return false;
                    VirtualThingManager.Add(thing, parentThing);
                    changes?.CollectionAdd(thing, parentThing);
                }
            }
            else //若非可堆叠物
            {
                if (!RemoveFromContainer(entity, changes) && OwHelper.GetLastError() != ErrorCodes.NO_ERROR) return false;
                var parentThing = container.GetThing();
                if (parentThing is null) return false;
                var thing = entity.GetThing();
                if (thing is null) return false;
                VirtualThingManager.Add(thing, parentThing);
                changes?.CollectionAdd(entity, container);
            }
            return true;
        }

        #endregion 改变实体相关功能

        #region 计算卡池相关

        /// <summary>
        /// 用池子指定的规则生成所有项。
        /// </summary>
        /// <param name="dice"></param>
        /// <returns></returns>
        public IEnumerable<GameDiceItem> GetOutputs(GameDice dice)
        {
            var list = new HashSet<GameDiceItem>();
            var rnd = new Random { };
            while (list.Count < dice.MaxCount)
            {
                var tmp = GetOutputs(dice.Items, rnd);
                if (dice.AllowRepetition)
                    list.Add(tmp);
                else if (!list.Contains(tmp))
                    list.Add(tmp);
            }
            return list;
        }

        /// <summary>
        /// 用随机数获取指定的池项中的随机一项。
        /// </summary>
        /// <param name="items"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public GameDiceItem GetOutputs(IEnumerable<GameDiceItem> items, Random random = null)
        {
            var totalWeight = items.Sum(c => c.Weight);    //总权重
            random ??= new Random();
            var total = (decimal)random.NextDouble() * totalWeight;
            foreach (var item in items)
            {
                if (item.Weight <= decimal.Zero) continue; //容错
                if (total <= item.Weight) return item;
                total -= item.Weight;
            }
            return default;
        }

        #endregion 计算卡池相关
    }
}
