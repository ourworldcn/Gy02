using GY02.Base;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GY02.Managers
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
        /// <param name="ignore">是否忽略可以忽略的项。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMatch(GameEntity entity, GameThingPrecondition conditions, bool ignore = false) =>
            conditions.Any(c => IsMatch(entity, c, ignore));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="condition"></param>
        /// <param name="ignore"></param>
        /// <returns></returns>
        public bool IsMatch(GameEntity entity, GameThingPreconditionItem condition, bool ignore = false)
        {
            if (ignore && condition.IgnoreIfDisplayList) return true;   //若忽略此项
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
            if (!condition.GeneralConditional.All(c => c.IsMatch(entity, ignore)))  //若通用属性要求的条件不满足
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
            var containerThing = container.GetThing();
            if (containerThing is null) goto lbErr;
            var tmp = containerThing.Children.FirstOrDefault(c => c.ExtraGuid == entity.TemplateId);  //可能的合成物
            if (tmp is null) goto noMerge;   //若不能合并
            var tt = _TemplateManager.GetFullViewFromId(tmp.ExtraGuid);
            if (tt is null) goto lbErr;   //若无法找到模板
            if (!tt.IsStk()) goto noMerge;  //若不可堆叠
            else if (tt.Stk != -1)   //若不可无限堆叠
            {
                var entity2 = GetEntity(tmp);
                if (entity2 is null) goto lbErr; //若无法获取实体对象
                if (entity.Count + entity2.Count > tt.Stk) goto noMerge;    //若不可合并
                else
                {
                    dest = GetEntity(tmp);
                    return true;
                }
            }
            else //无限堆叠
            {
                dest = GetEntity(tmp);
                return true;
            }
        lbErr:    //出错返回
            dest = null;
            return false;
        noMerge:    //不可合并
            OwHelper.SetLastError(ErrorCodes.NO_ERROR);
            dest = default;
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

        /// <summary>
        /// 为指定实体在一组实体中选择默认的容器对象。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="containeres"></param>
        /// <returns>null表示出错。</returns>
        public GameEntity GetDefaultContainer(GameEntity entity, IEnumerable<GameEntity> containeres)
        {
            var tt = _TemplateManager.GetFullViewFromId(entity.TemplateId); //获取模板
            var ptid = tt.ParentTId;
            var parent = containeres.Where(c => c.TemplateId == ptid);
            var count = parent.Count();
            if (count > 1)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"在指定集合中找到多个符合条件的默认容器，容器TId={ptid}");
                return null;
            }
            else if (count <= 0)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"在指定集合中找不到符合条件的默认容器，容器TId={ptid}");
                return null;
            }
            return parent.First();
        }

        /// <summary>
        /// 获取指定实体的容器或默认容器。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="containerTId">指定的容器TId，null则取默认容器。</param>
        /// <param name="containeres">仅在此集合内查找容器。</param>
        /// <returns>null表示出错，此时用<see cref="OwHelper.GetLastError"/>获取详细信息。</returns>
        public GameEntity GetContainer(GameEntity entity, Guid? containerTId, IEnumerable<GameEntity> containeres)
        {
            if (containerTId is null)    //若找默认容器
                return GetDefaultContainer(entity, containeres);
            var parent = containeres.Where(c => c.TemplateId == containerTId);
            var count = parent.Count();
            if (count > 1)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"在指定集合中找到多个符合条件的容器，容器TId={containerTId}");
                return null;
            }
            else if (count <= 0)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"在指定集合中找不到符合条件的容器，容器TId={containerTId}");
                return null;
            }
            return parent.First();
        }

        /// <summary>
        /// 获取指定角色下所有实体的枚举子。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>所有子虚拟物的枚举子。如果出错则返回null,此时用<see cref="OwHelper.GetLastError"/>确定具体信息。</returns>
        public IEnumerable<GameEntity> GetAllEntity(GameChar gameChar) => gameChar.GetAllChildren()?.Select(c => GetEntity(c));

        #endregion 基础功能

        #region 通用条件相关

        #endregion 通用条件相关

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
            if (parent is null) return false;   //若没有找到指定实体的父
            var db = _VirtualThingManager.GetDbContext(thing);
            if (db is null) return false;
            var result = _VirtualThingManager.Delete(thing, db);
            if (parent is not null)
            {
                changes?.CollectionRemove(entity, parent);
            }
            return result;
        }

        #endregion 删除实体相关功能

        #region 改变实体相关功能

        /// <summary>
        /// 设置等级和相关的序列属性。
        /// </summary>
        /// <param name="entity">有Level属性的实体。</param>
        /// <param name="newLevel"></param>
        /// <returns></returns>
        public bool SetLevel(GameEntity entity, int newLevel, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var tt = GetTemplate(entity);
            if (tt is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"无法找到对象(Id={entity.Id})的模板。");
                return false;
            }
            var oldLv = Convert.ToInt32(entity.Level);
            var pis = TypeDescriptor.GetProperties(tt).OfType<PropertyDescriptor>().Where(c => c.PropertyType.IsAssignableTo(typeof(IList<decimal>)));
            var pis2 = TypeDescriptor.GetProperties(entity).OfType<PropertyDescriptor>();
            var coll = pis.Join(pis2, c => c.Name, c => c.Name, (l, r) => (seq: l, prop: r));
            foreach (var pi in coll)
            {
                var seq = pi.seq.GetValue(tt) as IList<decimal>;
                if (seq is null)    //若该属性未设置
                    continue;   //忽略该序列
                var oldVal = seq[oldLv];
                var newVal = seq[newLevel];
                if (oldVal != newVal)
                    entity.SetPropertyValue(pi.prop, newVal - oldVal, changes);
            }
            //设置级别属性值
            entity.Level = newLevel;
            changes?.Add(new GamePropertyChangeItem<object>
            {
                Object = entity,
                PropertyName = nameof(entity.Level),
                OldValue = oldLv,
                HasOldValue = true,
                NewValue = newLevel,
                HasNewValue = true,
            });
            return true;
        }

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
        /// 
        /// </summary>
        /// <param name="source">(实体的模板Id，数量，强行指定的容器Id。)</param>
        /// <param name="gameChar"></param>
        /// <param name="changes"></param>
        /// <returns></returns>
        public bool CreateAndMove(IEnumerable<(Guid, decimal, Guid?)> source, GameChar gameChar, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var allEntity = GetAllEntity(gameChar)?.ToArray();
            if (allEntity is null) return false;

            var result = Create(source.Select(c => (c.Item1, c.Item2)));
            var coll = source.Zip(result);
            foreach (var item in coll)
            {
                var container = GetContainer(item.Second, item.First.Item3, allEntity);
                Move(item.Second, container, changes);
            }
            return true;
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

        /// <summary>
        /// 移动物品到指定容器中。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="container"></param>
        /// <param name="changes"></param>
        /// <returns></returns>
        public bool Move(GameEntity entity, GameEntity container, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var tt = _TemplateManager.GetFullViewFromId(entity.TemplateId);
            if (tt is null) return false;
            if (tt.IsStk())  //若是可堆叠物
            {
                if (!RemoveFromContainer(entity, changes) && OwHelper.GetLastError() != ErrorCodes.NO_ERROR) return false;
                if (IsMerge(entity, container, out var dest))  //若找到可合并物
                {
                    //Delete(entity);
                    Modify(dest, entity.Count, changes);
                }
                else //没有可合并物
                {
                    var parentThing = container.GetThing();
                    if (parentThing is null) return false;
                    var thing = entity.GetThing();
                    if (thing is null) return false;
                    VirtualThingManager.Add(thing, parentThing);
                    changes?.CollectionAdd(entity, container);
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

    }
}
