using Gy02.Publisher;
using Gy02Bll.Base;
using Gy02Bll.Managers;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.SyncCommand;

namespace Gy02Bll.Commands
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
            _CommandManager = commandManager;
            _Store = store;
            _GameEntityManager = gameEntityManager;
        }

        TemplateManager _TemplateManager;
        SyncCommandManager _CommandManager;
        GameAccountStore _Store;
        GameEntityManager _GameEntityManager;

        GameEntity GetDefaultContainer(GameEntity entity, GameChar gc)
        {
            var fv = _TemplateManager.Id2FullView.GetValueOrDefault(entity.TemplateId);
            var ptid = fv.ParentTId;
            var thing = (VirtualThing)entity.Thing;
            var charThing = gc?.Thing as VirtualThing ?? thing.GetGameCharThing();
            var parent = charThing.GetAllChildren().FirstOrDefault(c => c.ExtraGuid == ptid);
            if (parent is null) return null;
            return _TemplateManager.GetEntityBase(parent, out _) as GameEntity;
        }

        public override void Handle(MoveEntitiesCommand command)
        {
            if (!Verify(command)) return;
            var modifyItems = new List<(GameEntity, decimal)>();

            foreach (var item in command.Items)
            {
                var container = command.Container ?? GetDefaultContainer(item, command.GameChar);
                if (_GameEntityManager.IsMerge(item, container, out var dest))
                {
                    modifyItems.Add((dest, item.Count));
                }
                else //无法合并
                {
                    Move(item, container, command.Changes);
                }
            }
            if (modifyItems.Count > 0)
            {
                var subCommand = new ModifyEntityCountCommand
                {
                    Changes = command.Changes,
                    Items = modifyItems
                };
                foreach (var item in modifyItems)
                {
                    if (!_GameEntityManager.Modify(item.Item1, item.Item2, command.Changes))
                    {
                        command.FillErrorFromWorld();
                        return;
                    }
                }
                //_CommandManager.Handle(subCommand);
                //if (subCommand.HasError)
                //    command.FillErrorFrom(subCommand);
            }
            _Store.Save(command.GameChar.GetUser().GetKey());
        }

        bool Verify(MoveEntitiesCommand command)
        {
            var count = command.Items.Count(c => !_GameEntityManager.IsMerge(c, command.Container ?? GetDefaultContainer(c, command.GameChar), out _));
            var tmp = command.Container;

            if (tmp is GameContainer gi && gi.Capacity != -1 && count + ((VirtualThing)command.Container.Thing).Children.Count > gi.Capacity)
            {
                command.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                command.DebugMessage = "试图把过多的物品移动到指定容器中。";
                return false;
            }
            return true;
        }

        /// <summary>
        /// 移动虚拟物并记录变化数据。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="container"></param>
        /// <param name="changes">省略或为null则不记录变化数据。</param>
        public static void Move(OwGameEntityBase entity, OwGameEntityBase container, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var entityThing = (VirtualThing)entity.Thing;
            var oldContainer = entityThing.Parent;  //老容器
            if (oldContainer is not null)   //若有老容器
            {
                oldContainer.Children.Remove((VirtualThing)entity.Thing);
                changes?.CollectionRemove(entity, oldContainer);
            }
            //移动数据
            var containerThing = (VirtualThing)container.Thing;
            containerThing.Children.Add((VirtualThing)entity.Thing);
            entityThing.Parent = containerThing;
            entityThing.ParentId = containerThing.Id;
            changes?.CollectionAdd(entity, container);
        }

        /// <summary>
        /// 移动虚拟物并记录变化数据。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="container"></param>
        /// <param name="changes">省略或为null则不记录变化数据。</param>
        public static void Move(VirtualThing thing, VirtualThing container, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var parent = thing.Parent;
            if (parent is not null)
            {
                parent.Children.Remove(thing);
                changes?.CollectionRemove(thing, parent);
            }
            container.Children.Add(thing);
            thing.Parent = container;
            thing.ParentId = container.Id;
            changes?.CollectionAdd(thing, container);
        }

    }
}
