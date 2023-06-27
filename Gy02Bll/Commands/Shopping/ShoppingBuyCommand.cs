using GY02.Managers;
using GY02.Publisher;
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
    public class ShoppingBuyCommand : PropertyChangeCommandBase, IGameCharCommand
    {
        public ShoppingBuyCommand()
        {

        }

        public GameChar GameChar { get; set; }

        /// <summary>
        /// 购买的商品项Id。
        /// </summary>
        public Guid ShoppingItemTId { get; set; }
    }

    public class ShoppingBuyHandler : SyncCommandHandlerBase<ShoppingBuyCommand>, IGameCharHandler<ShoppingBuyCommand>
    {

        public ShoppingBuyHandler(GameAccountStoreManager accountStore, GameShoppingManager shoppingManager, GameEntityManager entityManager, GameBlueprintManager blueprintManager, GameDiceManager diceManager, SpecialManager specialManager)
        {
            AccountStore = accountStore;
            _ShoppingManager = shoppingManager;
            _EntityManager = entityManager;
            _BlueprintManager = blueprintManager;
            _DiceManager = diceManager;
            _SpecialManager = specialManager;
        }

        public GameAccountStoreManager AccountStore { get; }

        GameEntityManager _EntityManager;
        GameBlueprintManager _BlueprintManager;
        GameShoppingManager _ShoppingManager;
        GameDiceManager _DiceManager;
        SpecialManager _SpecialManager;

        public override void Handle(ShoppingBuyCommand command)
        {
            var key = ((IGameCharHandler<ShoppingBuyCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<ShoppingBuyCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            var tt = _ShoppingManager.GetShoppingTemplateByTId(command.ShoppingItemTId);
            //var si = _ShoppingManager.GetShoppingItemByTId(command.ShoppingItemTId);
            if (tt is null) goto lbErr;
            var now = OwHelper.WorldNow;
            if (!_ShoppingManager.IsValid(command.GameChar, tt, now, out _)) goto lbErr;

            var allEntity = _EntityManager.GetAllEntity(command.GameChar)?.ToArray();
            if (allEntity is null) goto lbErr;
            //提前缓存产出项
            var list = new List<(GameEntitySummary, IEnumerable<GameEntitySummary>)> { };
            if (tt.ShoppingItem.Outs.Count > 0) //若有产出项
            {
                var b = _SpecialManager.Transformed(tt.ShoppingItem.Outs, list, new EntitySummaryConverterContext
                {
                    Change = null,
                    GameChar = command.GameChar,
                    IgnoreGuarantees = false,
                    Random = new Random(),
                });
                if (!b) goto lbErr;
            }
            //消耗项
            if (tt.ShoppingItem.Ins.Count > 0)  //若需要消耗资源
                if (!_BlueprintManager.Deplete(allEntity, tt.ShoppingItem.Ins, command.Changes)) goto lbErr;

            if (!_EntityManager.CreateAndMove(list.SelectMany(c => c.Item2), command.GameChar, command.Changes)) goto lbErr;
            command.GameChar.ShoppingHistory.Add(new GameShoppingHistoryItem
            {
                Count = 1,
                DateTime = now,
                TId = command.ShoppingItemTId
            });
            AccountStore.Save(key);
            return;
        lbErr:
            command.FillErrorFromWorld();
        }
    }

    /// <summary>
    /// 处理累计签到的占位符Count+1的逻辑。
    /// </summary>
    public class CharFirstLoginedHandler : SyncCommandHandlerBase<CharFirstLoginedCommand>
    {
        public CharFirstLoginedHandler(GameEntityManager entityManager, GameShoppingManager shoppingManager, GameAccountStoreManager accountStore)
        {
            _EntityManager = entityManager;
            _ShoppingManager = shoppingManager;
            _AccountStore = accountStore;
        }

        GameEntityManager _EntityManager;
        GameShoppingManager _ShoppingManager;
        GameAccountStoreManager _AccountStore;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        public override void Handle(CharFirstLoginedCommand command)
        {
            var gc = command.GameChar;
            var allEntity = _EntityManager.GetAllEntity(gc).ToLookup(c => c.TemplateId);

            var slot = allEntity[ProjectContent.LeijiQiandaoSlotTId].Single();
            var coll = from tmp in gc.ShoppingHistory
                       where tmp.DateTime.Date.AddDays(1) <= command.LoginDateTimeUtc.Date //往日购买记录
                       let tt = _ShoppingManager.GetShoppingTemplateByTId(tmp.TId) //模板
                       where tt.Genus.Contains("gs_leijiqiandao")   //累计签到项
                       select tmp;
            var key = gc.Key;
            if (coll.Any() && slot.CountOfLastModifyUtc.HasValue)  //若曾买过累计签到项
            {
                if (coll.Max(c => c.DateTime).Date == slot.CountOfLastModifyUtc.Value.Date)
                {
                    slot.Count++;
                    _AccountStore.Save(key);
                }
            }

            slot = allEntity[ProjectContent.SevenDayQiandaoSlotTId].Single();
            coll = from tmp in gc.ShoppingHistory
                   where tmp.DateTime.Date.AddDays(1) == command.LoginDateTimeUtc.Date //昨日购买记录
                   let tt = _ShoppingManager.GetShoppingTemplateByTId(tmp.TId) //模板
                   where tt.Genus.Contains("gs_qiandao")   //7日签到项
                   select tmp;
            if (coll.Any() && slot.CountOfLastModifyUtc.HasValue)  //若往日买过累计签到项
            {
                if (coll.Max(c => c.DateTime).Date == slot.CountOfLastModifyUtc.Value.Date)
                {
                    slot.Count++;
                    _AccountStore.Save(key);
                }
            }
            //增加累计登陆天数
            slot = allEntity[ProjectContent.LoginedDayTId]?.FirstOrDefault();
            if (slot is not null)
                slot.Count++;
        }
    }

}

