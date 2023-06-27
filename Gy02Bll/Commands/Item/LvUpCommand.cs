using GY02.Base;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    public class LvUpCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public LvUpCommand() { }

        /// <summary>
        /// 发出此操作的角色。
        /// </summary>
        public GameChar GameChar { get; set; }

        public List<Guid> Ids { get; set; } = new List<Guid>();
    }

    public class LvUpCommandHandler : SyncCommandHandlerBase<LvUpCommand>, IGameCharHandler<LvUpCommand>
    {

        public LvUpCommandHandler(GameTemplateManager templateManager, SyncCommandManager syncCommandManager, GameEntityManager gameEntityManager, GameAccountStoreManager accountStore)
        {
            _TemplateManager = templateManager;
            _SyncCommandManager = syncCommandManager;
            _GameEntityManager = gameEntityManager;
            AccountStore = accountStore;
        }

        GameTemplateManager _TemplateManager;
        LvUpCommand _Command;
        SyncCommandManager _SyncCommandManager;
        GameEntityManager _GameEntityManager;

        public GameAccountStoreManager AccountStore { get; }

        int GetMaxLevel(TemplateStringFullView template)
        {
            var result = Math.Min(template.Atk?.Length ?? int.MaxValue, template.Def?.Length ?? int.MaxValue);
            result = Math.Min(result, template.Pow?.Length ?? int.MaxValue);
            return result - 1;
        }

        public override void Handle(LvUpCommand command)
        {
            var key = ((IGameCharHandler<LvUpCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<LvUpCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            _Command = command;
            var entities = _TemplateManager.GetEntityAndTemplateFullView<GameEntity>(command.GameChar, command.Ids);
            foreach (var entity in entities)
            {
                var tt = _TemplateManager.GetFullViewFromId(entity.TemplateId); //模板
                var ttUp = _TemplateManager.GetFullViewFromId(tt.LvUpTId ?? Guid.Empty);
                if (ttUp is null)
                {
                    command.FillErrorFromWorld();
                    return;
                }
                var lv = (int)entity.Level;
                if (ttUp.LvUpData[0].Counts.Count - 1 <= lv || GetMaxLevel(tt) <= lv)    //若超越等级
                {
                    command.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                    command.DebugMessage = $"物品等级达到限制，Id={entity.Id}";
                    return;
                }
            }
            foreach (var entity in entities)
            {
                _TemplateManager.SetTemplate((VirtualThing)entity.Thing);
                LvUp(entity, command.Changes);
            }
            AccountStore.Save(key);
        }

        public bool LvUp(GameEntity entity, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var tmpTfv = entity.GetTemplate();
            var gc = _Command.GameChar;
            if (tmpTfv?.LvUpTId is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"对象(Id={entity.Id})没有升级模板数据。");
                return false;
            }
            var tfv = _TemplateManager.Id2FullView.GetValueOrDefault(tmpTfv.LvUpTId.Value); //升级用的模板
            //_TemplateManager.GetCost()
            var lvData = tfv?.LvUpData;
            if (lvData is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"对象(Id={entity.Id})没有升级模板数据。");
                return false;
            }
            var all = _GameEntityManager.GetCost(entity, gc.GetAllChildren().Select(c => _TemplateManager.GetEntityBase(c, out _)).OfType<GameEntity>());
            if (all is null)
            {
                _Command.FillErrorFromWorld();
                return false;
            }
            all?.RemoveAll(c => c.Item2 == 0);
            if (all.Count > 0) //需要消耗材料
            {
                var modifyCount = new ModifyEntityCountCommand { Items = all, };
                var newCost = all.Select(c => new GameEntitySummary    //记录耗材原始数据
                {
                    TId = c.Item1.TemplateId,
                    Count = c.Item2,
                    Id = c.Item1.Id,
                    ParentTId = c.Item1.GetThing().ParentId
                }).ToArray();

                _SyncCommandManager.Handle(modifyCount);
                if (modifyCount.HasError)
                {
                    _Command.FillErrorFrom(modifyCount);
                    return false;
                }
                modifyCount.Changes.ForEach(c => changes.Add(c));
                //记录耗材数据
                var old = entity.LvUpAccruedCost?.ToArray() ?? Array.Empty<GameEntitySummary>();
                GameEntitySummary[] totalCost;
                if (old.Length <= 0) //若没有耗材记录
                    totalCost = newCost;
                else
                    totalCost = newCost.GroupJoin(entity.LvUpAccruedCost, c => c.TId, c => c.TId, (l, r) => new GameEntitySummary
                    {
                        Count = Math.Abs(l.Count) + Math.Abs(r.Any() ? r.First().Count : 0),
                        ParentTId = l.ParentTId,
                        TId = l.TId,
                    }).ToArray();
                entity.LvUpAccruedCost?.Clear();
                entity.LvUpAccruedCost.AddRange(totalCost);
                changes?.Add(new GamePropertyChangeItem<object>   //记录以前消耗的耗材
                {
                    Object = entity,
                    PropertyName = nameof(entity.LvUpAccruedCost),

                    HasOldValue = old.Length > 0,
                    OldValue = old,
                    HasNewValue = true,
                    NewValue = totalCost,
                });
            }
            SetLevel(entity, Convert.ToInt32(entity.Level + 1),_GameEntityManager, changes);
            return true;
        }

        /// <summary>
        /// 设置等级和相关的序列属性。
        /// </summary>
        /// <param name="entity">信息完备的实体（设置了模板属性）。</param>
        /// <param name="newLevel"></param>
        /// <returns></returns>
        public static bool SetLevel(GameEntity entity, int newLevel, GameEntityManager entityManager, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var tfv = entityManager.GetTemplate(entity);
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
            return true;
        }

    }
}
