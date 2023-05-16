using GY02.Managers;
using GY02.Templates;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Commands.Shopping
{
    public class GetShoppingItemsCommand : SyncCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 页签过滤。如果这里有数据则仅返回这些页签下的商品项，若没有指定或为空集合，则返回所有页签的数据（这可能导致性能问题）
        /// </summary>
        public string[] Genus { get; set; }

        /// <summary>
        /// 返回的有效数据项。
        /// </summary>
        public List<GameShoppingItem> ShoppingItems { get; set; } = new List<GameShoppingItem>();
    }

    public class GetShoppingItemsHandler : SyncCommandHandlerBase<GetShoppingItemsCommand>, IGameCharHandler<GetShoppingItemsCommand>
    {
        public GetShoppingItemsHandler(GameAccountStore accountStore, TemplateManager templateManager, GameShoppingManager shoppingManager)
        {
            AccountStore = accountStore;
            _TemplateManager = templateManager;
            _ShoppingManager = shoppingManager;
        }

        public GameAccountStore AccountStore { get; }

        TemplateManager _TemplateManager;

        GameShoppingManager _ShoppingManager;

        public override void Handle(GetShoppingItemsCommand command)
        {
            var key = ((IGameCharHandler<GetShoppingItemsCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<GetShoppingItemsCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败
            //获取基础集合
            IEnumerable<TemplateStringFullView> baseColl;
            if (command.Genus is null || command.Genus.Length <= 0)    //若未指定页签
            {
                baseColl = _TemplateManager.Id2FullView.Where(c => c.Value.ShoppingItem is not null).Select(c => c.Value);
            }
            else //若指定了页签
            {
                HashSet<string> genus = new HashSet<string>(command.Genus);
                baseColl = _TemplateManager.Id2FullView.Where(c => c.Value.ShoppingItem is not null && genus.Overlaps(c.Value.Genus)).Select(c => c.Value);
            }
            //过滤
            DateTime nowUtc = DateTime.UtcNow;    //当前
            foreach (var item in baseColl)  //遍历基础集合
            {
                var b = _ShoppingManager.IsValid(command.GameChar, item, nowUtc, out var startUtc);
                if (!b) continue;   //若不符合条件
            }
        }
    }
}
