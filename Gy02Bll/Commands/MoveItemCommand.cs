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
    public class MoveItemsCommand : SyncCommandBase
    {
        /// <summary>
        /// 角色。
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 令牌。
        /// </summary>
        public Guid Token { get; set; }

        /// <summary>
        /// 要移动物品的Id集合。
        /// </summary>
        public List<Guid> ItemIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 要移动到的目标容器唯一Id。
        /// </summary>
        public Guid ContainerId { get; set; }
    }

    public class MoveItemsHandler : SyncCommandHandlerBase<MoveItemsCommand>
    {
        public MoveItemsHandler(GameAccountStore store)
        {
            _Store = store;
        }

        GameAccountStore _Store;
        public override void Handle(MoveItemsCommand command)
        {
        }
    }
}
