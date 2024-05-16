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
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GY02.Managers
{
    public class EntityChangedEventArgs : EventArgs
    {
        public EntityChangedEventArgs()
        {

        }

        public EntityChangedEventArgs(IEnumerable<GameEntity> entities)
        {
            Entities = entities;
        }

        public IEnumerable<GameEntity> Entities { get; internal set; }

        /// <summary>
        /// 操作的角色对象。
        /// </summary>
        public GameChar GameChar { get; set; }

    }

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
        public GameEntityManager(IOptions<GameEntityManagerOptions> options, ILogger<GameEntityManager> logger,
            GameTemplateManager templateManager, VirtualThingManager virtualThingManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
            _VirtualThingManager = virtualThingManager;
        }

        GameTemplateManager _TemplateManager;
        VirtualThingManager _VirtualThingManager;

        #region 事件相关

        /// <summary>
        /// 引发实体变化的通知。
        /// </summary>
        /// <param name="entities"></param>
        public void InvokeEntityChanged(IEnumerable<GameEntity> entities, GameChar gameChar)
        {
            //var gc = entities.First().GetThing().GetAncestor(c => (c as IDbQuickFind)?.ExtraGuid == ProjectContent.CharTId); //寻找角色对象
            EntityChanged?.Invoke(this, new EntityChangedEventArgs() { Entities = entities, GameChar = gameChar });
        }

        /// <summary>
        /// 当实体属性变化时发生的事件。
        /// </summary>
        public event EventHandler<EntityChangedEventArgs> EntityChanged;

        #endregion  事件相关

        /// <summary>
        /// 修改一组实体的数量，若有任何一个实体在减少数量后，数量值为负数，则不会减少任何实体的数量并立即返回错误。
        /// </summary>
        /// <remarks><paramref name="changes"/>若是枚举子，则会在修改数量之前将其内容放置在数组中。</remarks>
        /// <param name="values">(实体对象,增量数量) 增量数量正数标识增加，负数标识减少。</param>
        /// <param name="changes"></param>
        /// <returns></returns>
        public bool Modify(IEnumerable<(GameEntity, decimal)> values, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var errFirst = values.FirstOrDefault(c => c.Item1.Count + c.Item2 < 0);
            if (errFirst.Item1 is GameEntity entity) //若找到错误
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"实体数量过低,实体Id={errFirst.Item1.Id}");
                return false;
            }
            if (values is ICollection<(GameEntity, decimal)>)    //若已经是一个集合对象
                values.ForEach(c => Modify(c.Item1, c.Item2, changes));
            else //若仅仅是一个枚举子
                values.ToArray().SafeForEach(c => Modify(c.Item1, c.Item2, changes));
            return true;
        }

        /// <summary>
        /// 修改虚拟物的数量。不进行参数校验的修改数量属性。并根据需要返回更改数据。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="count">数量的增量，即正数为增加，负数为减少。0则立即返回成功。</param>
        /// <param name="changes"></param>
        /// <returns>true成功，否则返回false,此时用<see cref="OwHelper.GetLastError"/>获取详细信息。</returns>
        public bool Modify(GameEntity entity, decimal count, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            //if (count == 0) return true;
            var template = _TemplateManager.GetFullViewFromId(entity.TemplateId);
            if (template is null) return false;
            var oldCount = entity.Count;
            entity.Count += count;
            changes?.Add(new GamePropertyChangeItem<object>()
            {
                Object = entity,
                PropertyName = nameof(entity.Count),
                WorldDateTime = OwHelper.WorldNow,
                HasOldValue = true,
                OldValue = oldCount,
                HasNewValue = true,
                NewValue = entity.Count,
            });
            if (entity.Count == 0 && !template.Count0Reserved) //若需要删除
            {
                Delete(entity, changes);
            }
            InvokeEntityChanged(new GameEntity[] { entity }, null);
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
            if (entity.GetThing() is not VirtualThing thing)
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"指定实体没有父实体，实体Id={entity.Id}");
                return null;
            }
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
        /// 获取指定角色下所有实体的枚举子。包含角色自身。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>所有子虚拟物的枚举子。如果出错则返回null,此时用<see cref="OwHelper.GetLastError"/>确定具体信息。</returns>
        public IEnumerable<GameEntity> GetAllEntity(GameChar gameChar) => gameChar.GetAllChildren()?.Select(c => GetEntity(c)).Append(gameChar);

        /// <summary>
        /// 获取指示，确定摘要是否和指定实体匹配。
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool IsMatch(GameEntitySummary summary, GameEntity entity)
        {
            if (summary.Id.HasValue && summary.Id.Value != entity.Id) return false;
            if (summary.TId != entity.TemplateId) return false;
            if (summary.Count > entity.Count) return false;
            if (summary.ParentTId.HasValue && summary.ParentTId.Value != entity.GetThing()?.Parent?.ExtraGuid) return false;
            return true;
        }

        /// <summary>
        /// 获取指示匹配的一组实体。
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="entities"></param>
        /// <returns>如果没有匹配的实体则返回空集合。</returns>
        public IEnumerable<GameEntity> GetMatches(GameEntitySummary summary, IEnumerable<GameEntity> entities) => entities.Where(c => IsMatch(summary, c));

        #endregion 基础功能

        #region 创建实体相关功能

        /// <summary>
        /// 创建一个实体。
        /// </summary>
        /// <param name="summary">对可堆叠物可以是任何数量，对不可堆叠物只能是正整数。
        /// 只能创建最终实体，不能识别序列，骰子等动态实体。</param>
        /// <returns>null表示出错。此时调用<see cref="OwHelper.GetLastError()"/>获取详细信息。</returns>
        public virtual List<GameEntity> Create(GameEntitySummary summary)
        {
            var tt = _TemplateManager.GetFullViewFromId(summary.TId);
            if (tt is null) return null;
            List<GameEntity> result;
            if (tt.IsStk())  //可堆叠物
            {
                result = new List<GameEntity>();
                var tmp = _VirtualThingManager.Create(tt);
                if (tmp is null) return null;
                var entity = GetEntity(tmp);
                if (entity is null) return null;
                var oriCount = entity.Count;    //预读fcp
                entity.Count = summary.Count;   //可以是任何数
                var tmpi = entity.Count;
                if (tt.Genus?.Contains(ProjectContent.ExistsDayNumberGenus) ?? false)
                {
                    entity.CreateDateTime = OwHelper.WorldNow;
                    entity.Count = 0;
                }
                if (tt.Genus?.Contains(ProjectContent.AutoIncGenus) ?? false)
                {
                    entity.CreateDateTime = OwHelper.WorldNow;
                    entity.Count = 0;
                }
                result.Add(entity);
            }
            else //不可堆叠物
            {
                var count = (int)summary.Count;
                if (count != summary.Count)
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    OwHelper.SetLastErrorMessage($"创建不可堆叠物的数量必须是整数。TId={summary.TId}");
                    return null;
                }
                else if (count < 0)
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    OwHelper.SetLastErrorMessage($"创建不可堆叠物的数量必须是正数。TId={summary.TId}");
                    return null;
                }
                var tmp = _VirtualThingManager.Create(tt.TemplateId, count);
                if (tmp is null) return null;
                result = new List<GameEntity>(count);
                foreach (var thing in tmp)
                {
                    var tmpEntity = GetEntity(thing);
                    if (tmpEntity is null) return null;
                    var oriCount = tmpEntity.Count; //预读fcp
                    tmpEntity.Count = 1;
                    if (tt.Genus?.Contains(ProjectContent.ExistsDayNumberGenus) ?? false)
                    {
                        tmpEntity.CreateDateTime = OwHelper.WorldNow;
                        tmpEntity.Count = 0;
                    }
                    if (tt.Genus?.Contains(ProjectContent.AutoIncGenus) ?? false)
                    {
                        tmpEntity.CreateDateTime = OwHelper.WorldNow;
                        tmpEntity.Count = 0;
                    }
                    result.Add(tmpEntity);
                }
            }
            return result;
        }

        /// <summary>
        /// 创建实体。
        /// </summary>
        /// <param name="entitySummarys">可以是空集合，此时则立即返回空集合。
        /// 对不可堆叠物品，会创建多个对象，每个对象数量是1。
        /// 只能创建最终实体，不能识别序列，骰子等动态实体。</param>
        /// <returns>创建(实体预览,实体)的集合，任何错误导致返回null，此时用<see cref="OwHelper.GetLastError"/>获取详细信息。</returns>
        public List<(GameEntitySummary, GameEntity)> Create(IEnumerable<GameEntitySummary> entitySummarys)
        {
            var result = new List<(GameEntitySummary, GameEntity)> { };
            var coll = entitySummarys.TryToCollection();
            foreach (var item in coll)
            {
                var list = Create(item);
                if (list is null) return null;
                list.ForEach(c => result.Add((item, c)));
            }
            return result;
        }

        /// <summary>
        /// 初始化实体对象。将实体对象置为刚创建状态。
        /// </summary>
        /// <param name="entities"></param>
        public void InitializeEntity(IEnumerable<GameEntity> entities)
        {
            DateTime now = OwHelper.WorldNow;
            foreach (var entity in entities)
            {
                var tt = _TemplateManager.GetFullViewFromId(entity.TemplateId);
                foreach (var fcp in entity.Fcps)
                {
                    var ttFcp = tt.Fcps.GetValueOrDefault(fcp.Key);
                    var dt = now;
                    fcp.Value.SetLastValue(ttFcp.CurrentValue, ref dt);
                }
                if (tt.Genus?.Contains(ProjectContent.ExistsDayNumberGenus) ?? false)
                    if (entity.CreateDateTime is null)
                        entity.CreateDateTime = now;
                if (tt.Genus?.Contains(ProjectContent.AutoIncGenus) ?? false)
                {
                    if (entity.CreateDateTime is null)
                        entity.CreateDateTime = now;
                }
            }
        }
        #endregion 创建实体相关功能

        #region 删除实体相关功能

        /// <summary>
        /// 从数据库中删除指定实体及其宿主对象。
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
            var db = (thing.GetRoot() as VirtualThing)?.GetDbContext();
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
        /// <param name="entity">信息完备的实体（设置了模板属性）。</param>
        /// <param name="newLevel"></param>
        /// <returns></returns>
        public bool SetLevel(GameEntity entity, int newLevel, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var tfv = GetTemplate(entity);
            if (tfv is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"无法找到对象(Id={entity.Id})的模板。");
                return false;
            }
            var oldLv = Convert.ToInt32(entity.Level);
            var pis = TypeDescriptor.GetProperties(tfv).OfType<PropertyDescriptor>().Where(c => c.PropertyType.IsAssignableTo(typeof(IList<decimal>)));
            var pis2 = TypeDescriptor.GetProperties(entity).OfType<PropertyDescriptor>();
            var coll = pis.Join(pis2, c => c.Name, c => c.Name, (l, r) => (seq: l, prop: r));
            foreach (var pi in coll)
            {
                var seq = pi.seq.GetValue(tfv) as IList<decimal>;
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
            //引发变化事件
            InvokeEntityChanged(new GameEntity[] { entity }, entity.GetThing().GetGameCharThing().GetJsonObject<GameChar>());
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
        /// 按预览对象创建并移动到指定角色上。
        /// </summary>
        /// <param name="summaries">不指定容器则使用默认容器。空集合则不进行任何操作。</param>
        /// <param name="gameChar"></param>
        /// <param name="changes">省略或为null则不记录变化信息。</param>
        /// <returns></returns>
        public bool CreateAndMove(IEnumerable<GameEntitySummary> summaries, GameChar gameChar, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var allEntity = GetAllEntity(gameChar)?.ToArray();
            if (allEntity is null) return false;
            var coll = summaries.TryToCollection();

            var list = Create(coll);
            if (list is null) return false;
#if DEBUG
            var tmp = allEntity.FirstOrDefault(c => c.TemplateId == Guid.Parse("A92E5EE3-1D48-40A1-BE7F-6C2A9F0BC652"));
#endif
            foreach (var item in list)
            {
                var container = GetContainer(item.Item2, item.Item1.ParentTId, allEntity);
                Move(item.Item2, container, changes);
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
            var coll = entities.TryToCollection();
            foreach (var entity in coll)
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

        /// <summary>
        /// 将指定的一组实体，放入一个容器中。
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="container"></param>
        /// <param name="changes"></param>
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
        public virtual bool Move(GameEntity entity, GameEntity container, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var tt = _TemplateManager.GetFullViewFromId(entity.TemplateId);

            if (tt is null) return false;
            if (tt.IsStk())  //若是可堆叠物
            {
                if (!RemoveFromContainer(entity, changes) && OwHelper.GetLastError() != ErrorCodes.NO_ERROR) return false;
                if (IsMerge(entity, container, out var dest))  //若找到可合并物
                {
                    var oldLv = dest.Level;
                    Modify(dest, entity.Count, changes);
                    if (tt.Exp2LvSequence is decimal[] e2l && e2l.Length > 0)  //若需要转化等级
                    {
                        dest.RefreshLevel(e2l);
                        if (oldLv != dest.Level)
                            changes?.MarkChanges(dest, nameof(dest.Level), oldLv, dest.Level);
                    }
                }
                else //没有可合并物
                {
                    var parentThing = container.GetThing();
                    if (parentThing is null) return false;
                    var thing = entity.GetThing();
                    if (thing is null) return false;
                    VirtualThingManager.Add(thing, parentThing);
                    changes?.CollectionAdd(entity, container);
                    changes?.MarkChanges(entity, nameof(entity.Count), 0, entity.Count);

                    if (tt.Exp2LvSequence is decimal[] e2l && e2l.Length > 0)  //若需要转化等级
                    {
                        var oldLv = entity.Level;
                        entity.RefreshLevel(e2l);
                        if (oldLv != entity.Level)
                            changes?.MarkChanges(entity, nameof(entity.Level), oldLv, entity.Level);
                    }
                    InvokeEntityChanged(new GameEntity[] { entity }, null);
                }
            }
            else //若非可堆叠物
            {
                if (!RemoveFromContainer(entity, changes) && OwHelper.GetLastError() != ErrorCodes.NO_ERROR) return false;
                var parentThing = container.GetThing();
                if (parentThing is null) return false;
                var thing = entity.GetThing();
                if (thing is null) return false;
                if (tt.UniInCharOuts is not null)   //若需要唯一性验证
                {
                    var gc = (GameChar)GetEntity(container.GetThing().GetGameCharThing());
                    if (gc.GetAllChildren().Any(c => c.ExtraGuid == tt.TemplateId)) //若发现重复
                    {
                        if (!CreateAndMove(tt.UniInCharOuts, (GameChar)GetEntity(container.GetThing().GetGameCharThing()), changes)) return false;
                        return true;
                    }
                }
                VirtualThingManager.Add(thing, parentThing);
                changes?.CollectionAdd(entity, container);
                InvokeEntityChanged(new GameEntity[] { entity }, null);
            }
            return true;
        }

        #endregion 改变实体相关功能

    }

    public static class GameEntityManagerExtensions
    {
    }
}
