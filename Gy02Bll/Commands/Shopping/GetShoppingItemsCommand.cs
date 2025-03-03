using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.GameDb;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{

    public class GetShoppingItemsCommand : SyncCommandBase, IGameCharCommand
    {
        public GetShoppingItemsCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 页签过滤。如果这里有数据则仅返回这些页签下的商品项，若没有指定或为空集合，则返回所有页签的数据（这可能导致性能问题）
        /// </summary>
        public string[] Genus { get; set; }

        /// <summary>
        /// 返回的有效数据项。
        /// </summary>
        public List<ShoppingItemState> ShoppingItemStates { get; set; } = new List<ShoppingItemState>();
    }

    public class GetShoppingItemsHandler : SyncCommandHandlerBase<GetShoppingItemsCommand>, IGameCharHandler<GetShoppingItemsCommand>
    {
        public GetShoppingItemsHandler(GameAccountStoreManager accountStore, GameTemplateManager templateManager, GameShoppingManager shoppingManager, GameBlueprintManager blueprintManager, GameSearcherManager searcherManager, GameSqlLoggingManager sqlLoggingManager)
        {
            AccountStore = accountStore;
            _TemplateManager = templateManager;
            _ShoppingManager = shoppingManager;
            _BlueprintManager = blueprintManager;
            _SearcherManager = searcherManager;
            _SqlLoggingManager = sqlLoggingManager;
        }

        public GameAccountStoreManager AccountStore { get; }

        GameTemplateManager _TemplateManager;

        GameShoppingManager _ShoppingManager;

        GameBlueprintManager _BlueprintManager;
        GameSearcherManager _SearcherManager;

        GameSqlLoggingManager _SqlLoggingManager;

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
                baseColl = _TemplateManager.Id2FullView.Where(c => c.Value.ShoppingItem is not null && c.Value.Genus is not null && genus.Overlaps(c.Value.Genus)).Select(c => c.Value);
            }
            //刷新金猪周期
            if (_ShoppingManager.IsChanged(command.GameChar, "gs_jinzhu")) _ShoppingManager.JinzhuChanged(command.GameChar);
            //刷新礼包周期
            if (_ShoppingManager.IsChanged(command.GameChar, "gs_leijilibao"))_ShoppingManager.LibaoChanged(command.GameChar);
            //过滤
            DateTime nowUtc = OwHelper.WorldNow;    //当前
            List<(TemplateStringFullView, DateTime)> list = new List<(TemplateStringFullView, DateTime)>();
            foreach (var item in baseColl)  //遍历基础集合
            {
                var b = _ShoppingManager.IsMatchWithoutBuyed(command.GameChar, item, nowUtc, out var startUtc, 2);  //TO DO
                if (!b) continue;   //若不符合条件
                list.Add((item, startUtc));
            }
#if DEBUG
            var tmp = command.GameChar.GetAllChildren().FirstOrDefault(c => c.ExtraGuid== Guid.Parse("46542DE4-B8B8-4735-936C-856273B650F7"));
#endif
            var coll1 = list.Where(c => c.Item1.Genus.Contains("gs_meirishangdian")).ToArray();
            command.ShoppingItemStates.AddRange(list.Select(c =>
            {
                var tmp = new ShoppingItemState
                {
                    TId = c.Item1.TemplateId,
                    StartUtc = c.Item2,
                    EndUtc = c.Item2 + c.Item1.ShoppingItem.Period.ValidPeriod,
                    BuyedCount = command.GameChar.ShoppingHistoryV2.Where(history => history.TId == c.Item1.TemplateId && history.WorldDateTime >= c.Item2 && history.WorldDateTime < c.Item2 + c.Item1.ShoppingItem.Period.ValidPeriod).Sum(c => c.Count),
                };
                var per = _SearcherManager.GetPeriodIndex(c.Item1.ShoppingItem.Ins, command.GameChar, out _);
                if (per.HasValue) //若有自周期
                {
                    var newBuyedCount = command.GameChar.ShoppingHistoryV2.Where(history => history.TId == c.Item1.TemplateId)
                        .Where(c => c.PeriodIndex == per).Sum(c => c.Count);
                    tmp.BuyedCount = newBuyedCount;
                }
                return tmp;
            }));

        }
    }
}
