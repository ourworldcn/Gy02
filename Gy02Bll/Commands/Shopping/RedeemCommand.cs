using GY02.Managers;
using OW.Game;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    public class RedeemCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 兑换码。
        /// </summary>
        public string Code { get; set; }
    }

    public class RedeemCommandHandler : SyncCommandHandlerBase<RedeemCommand>, IGameCharHandler<RedeemCommand>
    {
        public RedeemCommandHandler(GameAccountStoreManager accountStore)
        {
            AccountStore = accountStore;
        }

        public GameAccountStoreManager AccountStore { get; }

        public override void Handle(RedeemCommand command)
        {

        }
    }
}
