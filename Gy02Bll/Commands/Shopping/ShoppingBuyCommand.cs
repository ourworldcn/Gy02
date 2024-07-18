using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using OW.DDD;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.GameDb;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    /// <summary>
    /// 购买商品。
    /// </summary>
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

        /// <summary>
        /// 购买的商品数量。
        /// 如果购买商品超过上限则返回错误，此时没有购买任何商品。
        /// </summary>
        public int Count { get; set; }
    }

    public class ShoppingBuyHandler : SyncCommandHandlerBase<ShoppingBuyCommand>, IGameCharHandler<ShoppingBuyCommand>
    {

        public ShoppingBuyHandler(GameAccountStoreManager accountStore, GameShoppingManager shoppingManager, GameEntityManager entityManager, GameBlueprintManager blueprintManager, GameDiceManager diceManager, SpecialManager specialManager, SyncCommandManager commandManager, GameAchievementManager achievementManager, GameSearcherManager searcherManager, GameSqlLoggingManager sqlLoggingManager)
        {
            AccountStore = accountStore;
            _ShoppingManager = shoppingManager;
            _EntityManager = entityManager;
            _BlueprintManager = blueprintManager;
            _DiceManager = diceManager;
            _SpecialManager = specialManager;
            _CommandManager = commandManager;
            _AchievementManager = achievementManager;
            _SearcherManager = searcherManager;
            _SqlLoggingManager = sqlLoggingManager;
        }

        public GameAccountStoreManager AccountStore { get; }

        GameAchievementManager _AchievementManager;

        GameEntityManager _EntityManager;
        GameBlueprintManager _BlueprintManager;
        GameShoppingManager _ShoppingManager;
        GameDiceManager _DiceManager;
        SpecialManager _SpecialManager;
        SyncCommandManager _CommandManager;
        GameSearcherManager _SearcherManager;
        GameSqlLoggingManager _SqlLoggingManager;

        public override void Handle(ShoppingBuyCommand command)
        {
            var key = ((IGameCharHandler<ShoppingBuyCommand>)this).GetKey(command);
            using var dw = ((IGameCharHandler<ShoppingBuyCommand>)this).LockGameChar(command);
            if (dw.IsEmpty) return; //若锁定失败

            var tt = _ShoppingManager.GetShoppingTemplateByTId(command.ShoppingItemTId);
            if (tt is null) goto lbErr;
#if DEBUG
            //if (tt.TemplateId == Guid.Parse("71567fb7-adfa-4443-94ee-46a48245ef9c")) ;
            var ss = command.GameChar.ChengJiuSlot.Children.FirstOrDefault(c => c.TemplateId == Guid.Parse("822b1d80-70fe-417d-baea-e9c2aacbdcd8"));
#endif
            var now = OwHelper.WorldNow;
            if (command.Count <= 0)
            {
                command.HasError = true;
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = $"购买商品数量需要大于0。";
                return;
            }

            if (!_ShoppingManager.IsMatch(command.GameChar, tt, now, out var periodStart)) goto lbErr;    //若不能购买
            var end = periodStart + tt.ShoppingItem.Period.ValidPeriod;
            //避免超量购买
            var r = _ShoppingManager.IsMatchOnlyCount(command.GameChar, tt, periodStart, end, out var buyedCount);
            if (tt.ShoppingItem.MaxCount < buyedCount + command.Count)
            {
                command.HasError = true;
                command.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                command.DebugMessage = $"购买商品数量超过限制。";
                return;
            }
            //刷新金猪周期
            if (_ShoppingManager.IsChanged(command.GameChar, "gs_jinzhu")) _ShoppingManager.JinzhuChanged(command.GameChar, command.Changes);
            //刷新礼包周期
            if (_ShoppingManager.IsChanged(command.GameChar, "gs_leijilibao")) _ShoppingManager.LibaoChanged(command.GameChar);
            for (int i = 0; i < command.Count; i++)
            {
                var allEntity = _EntityManager.GetAllEntity(command.GameChar)?.ToArray();
                if (allEntity is null) goto lbErr;
                //提前缓存产出项
                var list = new List<(GameEntitySummary, IEnumerable<GameEntitySummary>)> { };
                if (!_SpecialManager.Transformed(tt, list, command.GameChar)) goto lbErr;

                var periodIndex = _SearcherManager.GetPeriodIndex(tt.ShoppingItem.Ins, command.GameChar, out _); //提前获取自周期数
                //消耗项
                if (tt.ShoppingItem.Ins.Count > 0)  //若需要消耗资源
                    if (!_BlueprintManager.Deplete(allEntity, tt.ShoppingItem.Ins, command.Changes))
                        if (OwHelper.GetLastError() != ErrorCodes.NO_ERROR)
                            goto lbErr;
                var entitySummary = list.SelectMany(c => c.Item2).Where(c => _AchievementManager.GetTemplateById(c.TId) is null).ToArray();
                var achis = list.SelectMany(c => c.Item2).Where(c => _AchievementManager.GetTemplateById(c.TId) is not null).ToArray();   //成就/任务
                if (!_EntityManager.CreateAndMove(entitySummary, command.GameChar, command.Changes)) goto lbErr;

                foreach (var summary in achis)  //遍历任务/成就
                {
                    if (_AchievementManager.GetOrCreate(command.GameChar, summary.TId) is not GameAchievement achi) continue;
                    if (!_AchievementManager.IsValid(achi, command.GameChar, now)) continue;    //无效时则不处理
                    _AchievementManager.SetExperience(achi, achi.Count + summary.Count, new SimpleGameContext(Guid.Empty, command.GameChar, now, command.Changes));
                }
                //加入购买历史记录
                var historyItem = _ShoppingManager.CreateHistoryItem(command.GameChar);
                historyItem.TId = command.ShoppingItemTId;
                historyItem.Count = command.Count;
                historyItem.WorldDateTime = now;
                historyItem.PeriodIndex = periodIndex;
                _ShoppingManager.AddHistoryItem(historyItem, command.GameChar);

                AccountStore.Save(key);
            }
            //金猪开启
            if (command.ShoppingItemTId == Guid.Parse("e4b9d61c-d130-4c2e-aad3-e55dfd40be6d")) //若开启金猪
            {
                _ShoppingManager.IsChanged(command.GameChar, "gs_jinzhu");
            }
            if (tt.Genus.Contains("gs_tianfu"))  //若是天赋商品
            {
                //已计入周任务
            }
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
        public CharFirstLoginedHandler(GameEntityManager entityManager, GameShoppingManager shoppingManager, GameAccountStoreManager accountStore, GameSqlLoggingManager sqlLoggingManager)
        {
            _EntityManager = entityManager;
            _ShoppingManager = shoppingManager;
            _AccountStore = accountStore;
            _SqlLoggingManager = sqlLoggingManager;
        }

        GameEntityManager _EntityManager;
        GameShoppingManager _ShoppingManager;
        GameAccountStoreManager _AccountStore;
        GameSqlLoggingManager _SqlLoggingManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        public override void Handle(CharFirstLoginedCommand command)
        {
            var now = OwHelper.WorldNow;
            var gc = command.GameChar;
            var allEntity = _EntityManager.GetAllEntity(gc).ToLookup(c => c.TemplateId);
            var key = gc.Key;

            //增加累计签到天数
            var slot = allEntity[ProjectContent.LeijiQiandaoSlotTId].Single();  //累计签到占位符

            var coll = from tmp in gc.ShoppingHistoryV2
                       let tt = _ShoppingManager.GetShoppingTemplateByTId(tmp.TId) //模板
                       where tt.Genus.Contains("gs_leijiqiandao")   //累计签到项
                       select tmp;
            DateTime? buyDate = coll.Any() ? coll.Max(c => c.WorldDateTime.Date) : null; //最后购买时间
            DateTime? markDate = slot.ExtensionProperties.GetDateTimeOrDefault("LastMark");   //最后签到时间
            if (buyDate.HasValue && buyDate.Value.Date >= markDate.Value.Date)   //若需要增加计数
            {
                slot.Count++;
                slot.ExtensionProperties["LastMark"] = now.ToString();
                _AccountStore.Save(key);
            }
            //增加七日签到天数
            slot = allEntity[ProjectContent.SevenDayQiandaoSlotTId].FirstOrDefault();
            if (slot is not null)
            {
                coll = from tmp in gc.ShoppingHistoryV2
                       let tt = _ShoppingManager.GetShoppingTemplateByTId(tmp.TId) //模板
                       where tt.Genus.Contains("gs_qiandao")   //7日签到项
                       select tmp;
                buyDate = coll.Any() ? coll.Max(c => c.WorldDateTime.Date) : null; //最后购买时间
                markDate = slot.ExtensionProperties.GetDateTimeOrDefault("LastMark");   //最后签到时间
                if (buyDate.HasValue && buyDate.Value.Date >= markDate.Value.Date)   //若需要增加计数
                {
                    slot.Count++;
                    slot.ExtensionProperties["LastMark"] = now.ToString();
                    _AccountStore.Save(key);
                }
            }
            //增加累计登录天数
            slot = allEntity[ProjectContent.LoginedDayTId]?.FirstOrDefault();
            if (slot is not null)
            {
                slot.Count++;
                _EntityManager.InvokeEntityChanged(new GameEntity[] { slot }, gc);
                _AccountStore.Save(key);
            }
        }
    }
}

