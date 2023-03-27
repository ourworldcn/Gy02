using Gy02.Publisher;
using Gy02Bll.Templates;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    /// <summary>
    /// 移动虚拟事务命令。
    /// </summary>
    public class MoveThingsCommand : PropertyChangeCommandBase
    {
        public MoveThingsCommand() { }

        public VirtualThing GameChar { get; set; }

        /// <summary>
        /// 要移动的虚拟物。
        /// </summary>
        public List<VirtualThing> Items { get; set; }

        /// <summary>
        /// 移动到的容器。
        /// </summary>
        public VirtualThing Container { get; set; }
    }

    public class MoveThingsHandler : SyncCommandHandlerBase<MoveThingsCommand>
    {


        public MoveThingsHandler(TemplateManager templateManager)
        {
            _TemplateManager = templateManager;
        }

        TemplateManager _TemplateManager;
        public override void Handle(MoveThingsCommand command)
        {
            if (!Verify(command)) return;

        }

        bool Verify(MoveThingsCommand command)
        {
            var type = _TemplateManager.GetTypeFromTId(command.Container.ExtraGuid);
            var tmp = command.Container.GetJsonObject(type);
            if (tmp is GameContainer gi && gi.Capacity != -1 && command.Items.Count + command.Container.Children.Count > gi.Capacity)
            {
                command.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                command.DebugMessage = "试图把过多的物品移动到指定容器中。";
                return false;
            }
            return true;
        }

        void Move(VirtualThing thing, VirtualThing container, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var parent = thing.Parent;
            var view = thing.GetJsonObject(_TemplateManager.GetTypeFromTId(thing.ExtraGuid));
            if (parent is not null)
            {
                parent.Children.Remove(thing);
                changes?.Add(new GamePropertyChangeItem<object>
                {
                    Object = parent,
                    PropertyName = nameof(parent.Children),
                    HasOldValue = true,
                    OldValue = view,
                    HasNewValue = false,
                });
            }
            container.Children.Add(thing);
            thing.Parent = container;
            thing.ParentId = container.Id;
            changes?.Add(new GamePropertyChangeItem<object>
            {
                Object = container,
                PropertyName = nameof(parent.Children),
                HasOldValue = false,
                HasNewValue = true,
                NewValue = view,
            });
        }

        bool IsMerge(VirtualThing thing, VirtualThing container, out VirtualThing dest)
        {
            var tmp = container.Children.FirstOrDefault(c => c.ExtraGuid == thing.ExtraGuid);  //可能的合成物
            if (tmp is null)    //若不能合成
            {
                dest = null;
                return false;
            }
            var tt = _TemplateManager.GetRawTemplateFromId(tmp.ExtraGuid);
            var fv = tt.GetJsonObject<TemplateStringFullView>();
            dest = tmp;
            return true;
        }
    }
}
