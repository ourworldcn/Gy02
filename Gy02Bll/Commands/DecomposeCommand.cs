using Gy02.Publisher;
using Gy02Bll.Managers;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    public class DecomposeCommand : PropertyChangeCommandBase
    {
        /// <summary>
        /// 要分解的物品。
        /// </summary>
        public GameEntity Item { get; set; }

        /// <summary>
        /// 针对的角色对象。
        /// </summary>
        public GameChar GameChar { get; set; }
    }

    public class DecomposeHandler : SyncCommandHandlerBase<DecomposeCommand>
    {
        public DecomposeHandler(GameAccountStore gameAccountStore, GameEntityManager gameEntityManager)
        {
            _GameAccountStore = gameAccountStore;
            _GameEntityManager = gameEntityManager;
        }

        GameAccountStore _GameAccountStore;

        GameEntityManager _GameEntityManager;

        public override void Handle(DecomposeCommand command)
        {
            if (!(command.Item.CompositingAccruedCost?.Count > 0))
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = $"指定物品不能分解，Id={command.Item.Id}";
                return;
            }
            string key = command.GameChar.GetUser().GetKey();
            if (!_GameAccountStore.Lock(key))   //若锁定失败
            {
                command.FillErrorFromWorld();
                return;
            }
            using var dw = DisposeHelper.Create(_GameAccountStore.Unlock, key);
            if (dw.IsEmpty) //若锁定失败
            {
                command.FillErrorFromWorld();
                return;
            }

            var list = _GameEntityManager.Create(command.Item.CompositingAccruedCost.Select(c => (c.TId, c.Count)));
            if (list is null) goto lbErr;
            if(!_GameEntityManager.Modify(command.Item, -command.Item.Count, command.Changes)) goto lbErr;

            _GameEntityManager.Move(list, command.GameChar, command.Changes);

            return;
        lbErr:
            command.FillErrorFromWorld();
        }
    }

}
