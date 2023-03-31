﻿using Gy02.Publisher;
using Gy02Bll.Templates;
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

        public LvUpCommandHandler(TemplateManager templateManager)
        {
            _TemplateManager = templateManager;
        }

        TemplateManager _TemplateManager;
        LvUpCommand _Command;

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
            var tmpTfv = entity.GetTemplateStringFullView();
            if (tmpTfv?.LvUpTId is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"对象(Id={entity.Id})没有升级模板数据。");
                return false;
            }
            var tfv = _TemplateManager.Id2FullView.GetValueOrDefault(tmpTfv.LvUpTId.Value); //升级用的模板
            var lvData = tfv?.LvUpData;
            if (lvData is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"对象(Id={entity.Id})没有升级模板数据。");
                return false;
            }

            foreach (var item in lvData)
            {
                //_TemplateManager.IsMatch(, lvData[1])
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
            var tfv = entity.GetTemplateStringFullView();
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
