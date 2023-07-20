using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using OW.SyncCommand;
using System.Text.Json;

namespace GY02.Commands
{
    public class CreateOrderCommand : SyncCommandBase, IGameCharCommand
    {
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要购买的物品清单。
        /// </summary>
        public List<GameEntitySummary> BuyItems { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// 返回的字符串，原样传递给SDK当作透参即可。
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// 创建的订单。
        /// </summary>
        public GameShoppingOrder ShoppingOrder { get; set; }
    }

    public class CreateOrderHandler : SyncCommandHandlerBase<CreateOrderCommand>, IGameCharHandler<CreateOrderCommand>
    {
        public CreateOrderHandler(GameTemplateManager templateManager, GameShoppingManager shoppingManager, GameAccountStoreManager accountStore)
        {
            _TemplateManager = templateManager;
            _ShoppingManager = shoppingManager;
            AccountStore = accountStore;
        }

        GameTemplateManager _TemplateManager;
        GameShoppingManager _ShoppingManager;

        public GameAccountStoreManager AccountStore { get; }

        public override void Handle(CreateOrderCommand command)
        {
            var key = ((IGameCharHandler<CreateOrderCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<CreateOrderCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            var db = command.GameChar.GetUser().GetDbContext(); //用户拥有的数据库上下文
            var order = new GameShoppingOrder
            {
                Confirm1 = true,
                CustomerId = command.GameChar.GetThing().IdString,
            };
            foreach (var item in command.BuyItems)  //遍历货品
            {
                var tt = _TemplateManager.GetFullViewFromId(item.TId);
                if (tt is null) goto lbErr;
                if (!tt.Genus.Contains(ProjectContent.CurrencyBuyGenus))
                {
                    command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    command.DebugMessage = $"指定用法币购买的商品项没有允许用法币购买的标志{ProjectContent.CurrencyBuyGenus},TId={tt.TemplateId}";
                    return;
                }
                var detail = new GameShoppingOrderDetail    //订单详细项
                {
                    GoodsId = item.TId.ToString(),
                    Count = item.Count,
                };
                order.Detailes.Add(detail);
            }

            db.Add(order);
            command.ShoppingOrder = order;
            //var str = JsonSerializer.Serialize(order.Id);
            var str = _ShoppingManager.EncodeString(order.IdString);
            command.Result = str;
            AccountStore.Save(key);
            return;
        lbErr:
            command.FillErrorFromWorld();
            return;
        }
    }
}
