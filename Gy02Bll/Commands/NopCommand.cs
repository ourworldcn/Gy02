using Gy02Bll.Commands.Combat;
using Gy02Bll.Managers;
using OW.DDD;
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
    /// <summary>
    /// 通知心跳，驱逐内存中用户信息的计时器重新开始计时。
    /// </summary>
    public class NopCommand : SyncCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class NopHandler : SyncCommandHandlerBase<NopCommand>, IGameCharHandler<NopCommand>
    {
        public NopHandler(GameAccountStore accountStore)
        {
            _AccountStore = accountStore;
        }

        GameAccountStore _AccountStore;

        public GameAccountStore AccountStore => _AccountStore;

        public override void Handle(NopCommand command)
        {
            var key = ((IGameCharHandler<NopCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<NopCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败
            if (!_AccountStore.Nop(key))
                command.FillErrorFromWorld();
            _AccountStore.Save(key);
        }
    }
}
