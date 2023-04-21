using Gy02.Publisher;
using Gy02Bll.Commands.Combat;
using Gy02Bll.Managers;
using OW.Game;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Item
{
    public class AutoLvUpCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要自动升级的物品唯一Id。
        /// </summary>
        public Guid ItemId { get; set; }
    }

    public class AutoLvUpHandler : SyncCommandHandlerBase<AutoLvUpCommand>, IGameCharHandler<AutoLvUpCommand>
    {
        public AutoLvUpHandler(GameAccountStore accountStore, SyncCommandManager commandManager)
        {
            _AccountStore = accountStore;
            _CommandManager = commandManager;
        }

        GameAccountStore _AccountStore;
        SyncCommandManager _CommandManager;

        public GameAccountStore AccountStore => _AccountStore;

        public override void Handle(AutoLvUpCommand command)
        {
            var key = ((IGameCharHandler<AutoLvUpCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<AutoLvUpCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败
            LvUpCommand lvup;
            do
            {
                lvup = new LvUpCommand { };
                lvup.Ids.Add(command.ItemId);
                lvup.GameChar = command.GameChar;
                _CommandManager.Handle(lvup);
                if (!lvup.HasError)  //若无措
                    command.Changes.AddRange(lvup.Changes);
                else if (lvup.ErrorCode != ErrorCodes.ERROR_IMPLEMENTATION_LIMIT)  //若不是材料不够的错误
                {
                    command.FillErrorFrom(lvup);
                    return;
                }
            } while (!lvup.HasError);
            _AccountStore.Save(key);
        }
    }
}
