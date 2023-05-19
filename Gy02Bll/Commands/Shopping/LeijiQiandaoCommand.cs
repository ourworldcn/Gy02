using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using OW.Game;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Shopping
{
    /// <summary>
    /// 累计签到的命令。
    /// </summary>
    public class LeijiQiandaoCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }
    }

    public class LeijiQiandaoHandler : SyncCommandHandlerBase<LeijiQiandaoCommand>, IGameCharHandler<LeijiQiandaoCommand>
    {
        public LeijiQiandaoHandler(GameAccountStore accountStore, GameEntityManager entityManager)
        {
            AccountStore = accountStore;
            _EntityManager = entityManager;
        }

        public GameAccountStore AccountStore { get; }

        GameEntityManager _EntityManager;

        public override void Handle(LeijiQiandaoCommand command)
        {
            var key = ((IGameCharHandler<LeijiQiandaoCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<LeijiQiandaoCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败
            var slot = command.GameChar.GetAllChildren().Select(c => _EntityManager.GetEntity(c)).FirstOrDefault(c => c.TemplateId == ProjectContent.LeijiQiandaoSlotTId);
            if (slot is null)
            {
                command.ErrorCode = ErrorCodes.ERROR_INVALID_DATA;
                command.DebugMessage = $"找不到累计签到的占位符对象。";
                return;
            }
            var now = DateTime.UtcNow;
            DateTime? lastChange = null;
            var obj= slot.ExtensionProperties.GetValueOrDefault("LastChange");
            if (obj is JsonElement je)
                lastChange = je.GetDateTime();
            else if (obj is DateTime dt)
                lastChange = dt;
            if (lastChange is not null && lastChange.Value.Date == now.Date)    //若当日已经签到
            {
                command.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                command.DebugMessage = $"当日已经签到过。";
                return;
            }
            slot.ExtensionProperties["LastChange"] = now;
            slot.Count++;
            command.Changes.Add(new OW.Game.PropertyChange.GamePropertyChangeItem<object>
            {
                Object = slot,
                PropertyName = nameof(slot.Count),

                HasNewValue = true,
                NewValue = slot.Count,

                HasOldValue = true,
                OldValue = slot.Count - 1,
            });
            AccountStore.Save(key);
        }
    }
}
