using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Item
{
    public class LvDownCommand : PropertyChangeCommandBase
    {
        /// <summary>
        /// 降级物品所属角色。
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要降级的物品。
        /// </summary>
        public GameEntity Entity { get; set; }
    }

    public class LvDownHandler : SyncCommandHandlerBase<LvDownCommand>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="syncCommandManager"></param>
        public LvDownHandler(SyncCommandManager syncCommandManager, TemplateManager templateManager)
        {
            _SyncCommandManager = syncCommandManager;
            _TemplateManager = templateManager;
        }

        SyncCommandManager _SyncCommandManager;
        private TemplateManager _TemplateManager;

        public override void Handle(LvDownCommand command)
        {
            var totalCost = command.Entity.LvUpAccruedCost.ToArray();
            LvUpCommandHandler.SetLevel(command.Entity, 0, command.Changes);
            command.Entity.LvUpAccruedCost = new List<GameEntitySummary>();
            List<VirtualThing> list = new List<VirtualThing>();

            foreach (var item in totalCost) //退还材料
            {
                CreateVirtualThingCommand create = new CreateVirtualThingCommand { TemplateId = item.TId };
                _SyncCommandManager.Handle(create);
                var tmp = CreateVirtualThingHandler.CreateThing(item.TId, Math.Abs(item.Count), _TemplateManager, _SyncCommandManager);
                if (tmp is null)
                {
                    command.FillErrorFromWorld();
                    return;
                }
                list.AddRange(tmp);
            }

            var move = new MoveEntitiesCommand { GameChar = command.GameChar, Items = list.Select(c => (GameEntity)_TemplateManager.GetEntityBase(c, out _)).ToList() };
            _SyncCommandManager.Handle(move);
            command.FillErrorFrom(move);
            if (!command.HasError)
            {
                command.Changes?.AddRange(move.Changes);
                command.Changes?.Add(new GamePropertyChangeItem<object>
                {
                    Object = command.Entity,
                    PropertyName = nameof(GameEntity.LvUpAccruedCost),
                    HasOldValue = totalCost.Length > 0,
                    OldValue = totalCost,
                    HasNewValue = false,
                });
            }
        }
    }
}
