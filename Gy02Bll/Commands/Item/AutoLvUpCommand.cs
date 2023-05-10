using GY02.Managers;
using GY02.Publisher;
using OW.Game;
using OW.Game.Entity;
using OW.SyncCommand;

namespace GY02.Commands
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
                if (!lvup.HasError)  //若无错
                    command.Changes.AddRange(lvup.Changes);
                else if (lvup.ErrorCode != ErrorCodes.ERROR_IMPLEMENTATION_LIMIT)  //若不是材料不够的错误或等级达到限制
                {
                    command.FillErrorFrom(lvup);
                    return;
                }
                else //已经没有材料或等级达到最大
                {
                    break;
                }
            } while (!lvup.HasError);
            _AccountStore.Save(key);
        }
    }
}
