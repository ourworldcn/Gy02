using Gy02.Publisher;
using Gy02Bll.Base;
using Gy02Bll.Templates;
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

namespace Gy02Bll.Commands
{
    public class LvUpCommand : PropertyChangeCommandBase
    {
        public LvUpCommand() { }

        /// <summary>
        /// 发出此操作的角色。
        /// </summary>
        public GameChar GameChar { get; set; }

        public List<Guid> Ids { get; set; } = new List<Guid>();
    }

    public class LvUpCommandHandler : SyncCommandHandlerBase<LvUpCommand>
    {

        public LvUpCommandHandler(TemplateManager templateManager, SyncCommandManager syncCommandManager)
        {
            _TemplateManager = templateManager;
            _SyncCommandManager = syncCommandManager;
        }

        TemplateManager _TemplateManager;
        LvUpCommand _Command;
        SyncCommandManager _SyncCommandManager;

        public override void Handle(LvUpCommand command)
        {
            _Command = command;
            var entities = _TemplateManager.GetEntityAndTemplateFullView<GameEntity>(command.GameChar, command.Ids);
            foreach (var entity in entities)
            {
                _TemplateManager.SetTemplate((VirtualThing)entity.Thing);
                LvUp(entity, command.Changes);
            }
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
            var all = _TemplateManager.GetCost(entity, gc.GetAllChildren().Select(c => _TemplateManager.GetEntityBase(c, out _)).OfType<GameEntity>());
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
            SetLevel(entity, Convert.ToInt32(entity.Level + 1), changes);
            return true;
        }

        /// <summary>
        /// 设置等级和相关的序列属性。
        /// </summary>
        /// <param name="entity">信息完备的实体（设置了模板属性）。</param>
        /// <param name="newLevel"></param>
        /// <returns></returns>
        public static bool SetLevel(GameEntity entity, int newLevel, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var tfv = entity.GetTemplate();
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
                var seq = (IList<decimal>)pi.seq.GetValue(tfv);
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
