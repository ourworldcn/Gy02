using GY02.Commands;
using GY02.Managers;
using OW.Game;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    public class GetShoppingListCommand : SyncCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 限定的页签名称，仅限定检索该页签下的数据。
        /// </summary>
        public string Genus { get; set; }

        /// <summary>
        /// 限定检索的组号，若没指定则会检索页签下所有数据。
        /// </summary>
        public int? GroupNubem { get; set; }
    }

    public class GetShoppingListHandler : SyncCommandHandlerBase<GetShoppingListCommand>, IGameCharHandler<GetShoppingListCommand>
    {
        public GetShoppingListHandler(GameAccountStore accountStore)
        {
            AccountStore = accountStore;
        }

        public GameAccountStore AccountStore { get; }

        public override void Handle(GetShoppingListCommand command)
        {
            var key = ((IGameCharHandler<GetShoppingListCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<GetShoppingListCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败
        }
    }
}
