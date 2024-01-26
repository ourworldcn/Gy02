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
        public GetShoppingItemsHandler(GameAccountStoreManager accountStore, GameTemplateManager templateManager, GameShoppingManager shoppingManager, GameBlueprintManager blueprintManager)
        {
            AccountStore = accountStore;
            _TemplateManager = templateManager;
            _ShoppingManager = shoppingManager;
            _BlueprintManager = blueprintManager;
        }

        public GameAccountStoreManager AccountStore { get; }

        GameTemplateManager _TemplateManager;

        GameShoppingManager _ShoppingManager;

        GameBlueprintManager _BlueprintManager;

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
            //过滤
            DateTime nowUtc = OwHelper.WorldNow;    //当前
            List<(TemplateStringFullView, DateTime)> list = new List<(TemplateStringFullView, DateTime)>();
            foreach (var item in baseColl)  //遍历基础集合
            {
                var b = _ShoppingManager.IsMatchWithoutBuyed(command.GameChar, item, nowUtc, out var startUtc, 2);
                if (!b) continue;   //若不符合条件
                list.Add((item, startUtc));
            }
            //var tmp = _TemplateManager.Id2FullView.Values.FirstOrDefault(c => c.TemplateId == Guid.Parse("e2d2115d-cee6-4f1a-b173-ab3b647307b7"));

            var coll1 = list.Where(c => c.Item1.Genus.Contains("gs_meirishangdian")).ToArray();

            command.ShoppingItemStates.AddRange(list.Select(c =>
            {
                var tmp = new ShoppingItemState
                {
                    TId = c.Item1.TemplateId,
                    StartUtc = c.Item2,
                    EndUtc = c.Item2 + c.Item1.ShoppingItem.Period.ValidPeriod,
                    BuyedCount = command.GameChar.ShoppingHistory.Where(history => history.TId == c.Item1.TemplateId && history.DateTime >= c.Item2 && history.DateTime < c.Item2 + c.Item1.ShoppingItem.Period.ValidPeriod).Sum(c => c.Count),
                };
                var per = _BlueprintManager.GetPeriodIndex(c.Item1.ShoppingItem.Ins, command.GameChar, out _);
                if (per.HasValue) //若有自周期
                {
                    var newBuyedCount = command.GameChar.ShoppingHistory.Where(history => history.TId == c.Item1.TemplateId && history.PeriodIndex == per).Sum(c => c.Count);
                    tmp.BuyedCount = Math.Max(tmp.BuyedCount, newBuyedCount);
                }
                return tmp;
            }));

        }
    }
}
