using Gy02.Publisher;
using Gy02Bll.Managers;
using MyNamespace;
using OW.Game.Entity;
using OW.Game.PropertyChange;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Combat
{
    public class EndCombatCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public EndCombatCommand()
        {

        }

        /// <summary>
        /// 战斗的角色对象。
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 战斗关卡的模板Id。
        /// </summary>
        public Guid CombatTId { get; set; }

        /// <summary>
        /// 掉落物品的集合。
        /// </summary>
        public List<GameEntitySummary> Rewards { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// 杀怪或其它集合。
        /// </summary>
        public List<GameEntitySummary> Others { get; set; } = new List<GameEntitySummary>();
    }

    /// <summary>
    /// 
    /// </summary>
    public class EndCombatHandler : SyncCommandHandlerBase<EndCombatCommand>, IGameCharHandler<EndCombatCommand>
    {
        public EndCombatHandler(GameAccountStore gameAccountStore, GameEntityManager gameEntityManager)
        {
            _GameAccountStore = gameAccountStore;
            _GameEntityManager = gameEntityManager;
        }

        GameAccountStore _GameAccountStore;
        GameEntityManager _GameEntityManager;

        public GameAccountStore AccountStore => _GameAccountStore;

        public override void Handle(EndCombatCommand command)
        {
            var key = ((IGameCharHandler<EndCombatCommand>)this).GetKey(command);
            var dw = ((IGameCharHandler<EndCombatCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            if (command.CombatTId != command.GameChar.CombatTId)
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = $"客户端指定战斗模板Id={command.CombatTId},但用户实际的战斗模板Id={command.GameChar.CombatTId}";
                return;
            }
            //把掉落物品增加到角色背包中
            var coll = from tmp in command.Rewards
                       group tmp by tmp.TId into g
                       where g.Count() > 0
                       select (TId: g.Key, Count: g.Sum(c => c.Count));
            var list = new List<GameEntity>();
            if (coll.Any())
            {
                list = _GameEntityManager.Create(coll);
                _GameEntityManager.Move(list, command.GameChar, command.Changes);
            }
            var change = new GamePropertyChangeItem<object>
            {
                Object = command.GameChar,
                PropertyName = nameof(command.GameChar.CombatTId),
                HasOldValue = true,
                OldValue = command.GameChar.CombatTId,
                HasNewValue = false,
                NewValue = null,
            };
            command.GameChar.CombatTId = null;
            command.Changes?.Add(change);
            _GameAccountStore.Save(key);
        }
    }
}
