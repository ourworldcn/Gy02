using Gy02.Publisher;
using OW.Game;
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

        public GameChar GameChar { get; set; }

        public List<GameEntity> Items { get; set; } = new List<GameEntity>();

        /// <summary>
        /// 指定容器，若省略或为null则会根据每个虚拟物的模板数据放入其默认容器。
        /// </summary>
        public GameEntity Container { get; set; }
    }

    public class MoveEntitiesHandler : SyncCommandHandlerBase<MoveEntitiesCommand>
    {


        public MoveEntitiesHandler(TemplateManager templateManager, SyncCommandManager commandManager)
        {
            _TemplateManager = templateManager;
            _CommandManager = commandManager;
        }

        TemplateManager _TemplateManager;
        SyncCommandManager _CommandManager;

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
                if (IsMerge(item, container, _TemplateManager, out var dest))
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
                _CommandManager.Handle(subCommand);
                if (subCommand.HasError)
                    command.FillErrorFrom(subCommand);
            }
        }

        bool Verify(MoveEntitiesCommand command)
        {
            var count = command.Items.Count(c => !IsMerge(c, command.Container ?? GetDefaultContainer(c, command.GameChar), _TemplateManager, out _));
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
        /// 获取指定虚拟物是否可以和指定容器中的虚拟物合并。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="container"></param>
        /// <param name="templateManager"></param>
        /// <param name="dest"></param>
        /// <returns>true指定容器中存在可合并的虚拟物，false没有可合并的虚拟物或出错，此时用<see cref="OwHelper.GetLastError"/>确定是否有错。</returns>
        public static bool IsMerge(GameEntity entity, GameEntity container, TemplateManager templateManager, out GameEntity dest)
        {
            var tmp = ((VirtualThing)container.Thing).Children.FirstOrDefault(c => c.ExtraGuid == entity.TemplateId);  //可能的合成物
            if (tmp is null) goto noMerge;    //若不能合并
            var tt = templateManager.Id2FullView.GetValueOrDefault(tmp.ExtraGuid);
            if (tt is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                dest = null;
                return false;
            }
            if (tt.Stk == 1)   //若不可堆叠
                goto noMerge;
            else if (tt.Stk != -1)   //若不可无限堆叠
            {
                var entity2 = (GameEntity)templateManager.GetEntityBase(tmp, out _);
                if (entity.Count + entity2.Count > tt.Stk) goto noMerge;    //若不可合并
                else
                {
                    dest = templateManager.GetEntityBase(tmp, out _) as GameEntity;
                    return true;
                }
            }
            else //无限堆叠
            {
                dest = templateManager.GetEntityBase(tmp, out _) as GameEntity;
                return true;
            }
        noMerge:
            dest = null;
            return false;
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
            if (oldContainer is not null)   //若没有老容器
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

        /// <summary>
        /// 获取指定虚拟物是否可以和指定容器中的虚拟物合并。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="container"></param>
        /// <param name="dest"></param>
        /// <returns>true指定容器中存在可合并的虚拟物，false没有可合并的虚拟物或出错，此时用<see cref="OwHelper.GetLastError"/>确定是否有错。</returns>
        bool IsMerge(VirtualThing thing, VirtualThing container, out VirtualThing dest)
        {
            var tmp = container.Children.FirstOrDefault(c => c.ExtraGuid == thing.ExtraGuid);  //可能的合成物
            if (tmp is null)    //若不能合并
            {
                dest = null;
                return false;
            }
            var tt = _TemplateManager.Id2FullView.GetValueOrDefault(tmp.ExtraGuid);
            if (tt is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                dest = null;
                return false;
            }
            if (tt.Stk != -1)   //若不可堆叠
            {
                var entity1 = _TemplateManager.GetEntityBase(thing, out _) as GameEntity;
                var entity2 = _TemplateManager.GetEntityBase(tmp, out _) as GameEntity;
                if (entity1.Count + entity2.Count > tt.Stk)
                {
                    dest = null;
                    return false;
                }
                else
                {
                    dest = tmp;
                    return true;
                }
            }
            else
            {
                dest = tmp;
                return true;
            }
        }

    }
}
