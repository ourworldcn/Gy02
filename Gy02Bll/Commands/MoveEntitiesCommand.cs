using GY02.Managers;
using GY02.Publisher;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.SyncCommand;

namespace GY02.Commands
{
    public class MoveEntitiesCommand : PropertyChangeCommandBase
    {
        public MoveEntitiesCommand() { }

        /// <summary>
        /// 物品目的地的角色（如果有）
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要移动的物品。
        /// </summary>
        public List<GameEntity> Items { get; set; } = new List<GameEntity>();

        /// <summary>
        /// 指定容器，若省略或为null则会根据每个虚拟物的模板数据放入其默认容器。
        /// </summary>
        public GameEntity Container { get; set; }
    }

    public class MoveEntitiesHandler : SyncCommandHandlerBase<MoveEntitiesCommand>
    {


        public MoveEntitiesHandler(TemplateManager templateManager, SyncCommandManager commandManager, GameAccountStore store, GameEntityManager gameEntityManager)
        {
            _TemplateManager = templateManager;
            _Store = store;
            _EntityManager = gameEntityManager;
        }

        TemplateManager _TemplateManager;
        GameAccountStore _Store;
        GameEntityManager _EntityManager;

        public override void Handle(MoveEntitiesCommand command)
        {
            _EntityManager.Move(command.Items, command.GameChar, command.Changes);
            _Store.Save(command.GameChar.GetUser().Key);
        }

    }
}
