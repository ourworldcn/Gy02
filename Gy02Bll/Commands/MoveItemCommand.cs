using Gy02.Publisher;
using Gy02Bll.Managers;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands
{
    public class MoveItemsCommand : SyncCommandBase
    {
        /// <summary>
        /// 角色。
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 令牌。
        /// </summary>
        public Guid Token { get; set; }

        /// <summary>
        /// 要移动物品的Id集合。
        /// </summary>
        public List<Guid> ItemIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 要移动到的目标容器唯一Id。
        /// </summary>
        public Guid ContainerId { get; set; }
    }

    public class MoveItemsHandler : SyncCommandHandlerBase<MoveItemsCommand>
    {
        public MoveItemsHandler(GameAccountStore store, TemplateManager templateManager)
        {
            _Store = store;
            _TemplateManager = templateManager;
        }

        GameAccountStore _Store;
        TemplateManager _TemplateManager;

        public override void Handle(MoveItemsCommand command)
        {
            using var dw = _Store.GetCharFromToken(command.Token, out var gameChar);
            if (dw.IsEmpty)
            {
                command.FillErrorFromWorld();
                return;
            }
            var things = (gameChar.Thing as VirtualThing).GetAllChildren().Join(command.ItemIds, c => c.Id, c => c, (l, r) => l).ToArray();
            var container = (gameChar.Thing as VirtualThing).GetAllChildren().FirstOrDefault(c => c.Id == command.ContainerId);
            if (container is null || things.Length != command.ItemIds.Count)
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                return;
            }
            var slot = container.GetJsonObject<GameSlot<GameItem>>();
            if (slot.Capacity != -1 && slot.Children.Count() + command.ItemIds.Count > slot.Capacity)
            {
                command.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                return;
            }
            foreach (var item in things)
            {
                Move(item, container);
            }
            _Store.Save(gameChar.GetUser().GetKey());
        }

        static void Move(VirtualThing thing, VirtualThing container)
        {
            var parent = thing.Parent;
            if (parent is not null)
                parent.Children.Remove(thing);
            container.Children.Add(thing);
            thing.Parent = container;
            thing.ParentId = container.Id;
        }


    }
}
