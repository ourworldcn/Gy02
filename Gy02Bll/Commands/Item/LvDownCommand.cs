using GY02.Managers;
using GY02.Templates;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.SyncCommand;

namespace GY02.Commands
{
    public class LvDownCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public LvDownCommand()
        {

        }
        /// <summary>
        /// 降级物品所属角色。
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要降级的物品。
        /// </summary>
        public GameEntity Entity { get; set; }
    }

    public class LvDownHandler : SyncCommandHandlerBase<LvDownCommand>, IGameCharHandler<LvDownCommand>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="syncCommandManager"></param>
        public LvDownHandler(SyncCommandManager syncCommandManager, GameTemplateManager templateManager, GameAccountStoreManager accountStore, GameEntityManager entityManager)
        {
            _SyncCommandManager = syncCommandManager;
            _TemplateManager = templateManager;
            AccountStore = accountStore;
            _EntityManager = entityManager;
        }

        SyncCommandManager _SyncCommandManager;
        private GameTemplateManager _TemplateManager;

        public GameAccountStoreManager AccountStore { get; }
        GameEntityManager _EntityManager;

        public override void Handle(LvDownCommand command)
        {
            var key = ((IGameCharHandler<LvDownCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<LvDownCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            var totalCost = command.Entity.LvUpAccruedCost.ToArray();
            _EntityManager.SetLevel(command.Entity, 0, command.Changes);
            command.Entity.LvUpAccruedCost = new List<GameEntitySummary>();
            List<VirtualThing> list = new List<VirtualThing>();

            foreach (var item in totalCost) //退还材料
            {
                var tmp = _EntityManager.Create(new GameEntitySummary { TId = item.TId, Count = Math.Abs(item.Count) });

                if (tmp is null)
                {
                    command.FillErrorFromWorld();
                    return;
                }
                list.AddRange(tmp.Select(c => c.GetThing()));
            }

            //var move = new MoveEntitiesCommand { GameChar = command.GameChar, Items = list.Select(c => (GameEntity)_TemplateManager.GetEntityBase(c, out _)).ToList() };
            //_SyncCommandManager.Handle(move);
            //command.FillErrorFrom(move);

            _EntityManager.Move(list.Select(c => _EntityManager.GetEntity(c)), command.GameChar, command.Changes);
            if (!command.HasError)
            {
                command.Changes?.Add(new GamePropertyChangeItem<object>
                {
                    Object = command.Entity,
                    PropertyName = nameof(GameEntity.LvUpAccruedCost),
                    HasOldValue = totalCost.Length > 0,
                    OldValue = totalCost,
                    HasNewValue = false,
                });
                AccountStore.Save(key);
            }
        }
    }
}
