using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.GameDb;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GY02.Managers
{
    /// <summary>
    /// 商城管理器的配置类。
    /// </summary>
    public class GameShoppingManagerOptions : IOptions<GameShoppingManagerOptions>
    {
        public GameShoppingManagerOptions Value => this;
    }

    /// <summary>
    /// 游戏商城管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameShoppingManager : GameManagerBase<GameShoppingManagerOptions, GameShoppingManager>
    {
        /// <summary>
        /// 购买记录的前缀字符串。
        /// </summary>
        public const string ShoppingBuyHistoryPrefix = "ShoppingBuy";

        public GameShoppingManager(IOptions<GameShoppingManagerOptions> options, ILogger<GameShoppingManager> logger, GameBlueprintManager blueprintManager,
            GameEntityManager entityManager, GameTemplateManager templateManager, GameSearcherManager searcherManager, GameSqlLoggingManager sqlLoggingManager) 
            : base(options, logger)
        {
            _BlueprintManager = blueprintManager;
            _EntityManager = entityManager;
            _TemplateManager = templateManager;
            _SearcherManager = searcherManager;
            _SqlLoggingManager = sqlLoggingManager;
        }

        GameBlueprintManager _BlueprintManager;
        GameEntityManager _EntityManager;
        GameTemplateManager _TemplateManager;
        GameSearcherManager _SearcherManager;
        GameSqlLoggingManager _SqlLoggingManager;

        #region 商品购买历史记录相关

        /// <summary>
        /// 获取指定角色购买行为的ActionId。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public string GetShoppingBuyHistoryActionId(GameChar gameChar)
        {
            return $"{ShoppingBuyHistoryPrefix}.{gameChar.GetThing().Base64IdString}";
        }

        /// <summary>
        /// 创建一个新的购买记录对象，并自动设置好ActionId属性。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public GameShoppingHistoryItemV2 CreateHistoryItem(GameChar gameChar)
        {
            var tmp = new ActionRecord
            {
                ActionId = GetShoppingBuyHistoryActionId(gameChar),
            };
            var result = GameShoppingHistoryItemV2.From(tmp);
            return result;
        }

        /// <summary>
        /// 获取指定用户购买记录的可查询对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="db">使用的数据库上下文。调用者必须自行释放。</param>
        /// <returns></returns>
        public IQueryable<ActionRecord> GetShoppingBuyHistoryQuery(GameChar gameChar, GY02LogginContext db)
        {
            var actionId = GetShoppingBuyHistoryActionId(gameChar);
            return db.ActionRecords.Where(c => c.ActionId == actionId);
        }

        public void SaveHistoryItem(GameShoppingHistoryItemV2 item)
        {
            item.Save();
            _SqlLoggingManager.Save(item.ActionRecord);
        }

        public void SaveHistoryItems(IEnumerable<GameShoppingHistoryItemV2> item)
        {
            item.ForEach(c => c.Save());
            _SqlLoggingManager.Save(item.Select(c => c.ActionRecord));
        }

        /// <summary>
        /// 追加集合，并保存到数据库。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="gameChar"></param>
        public void AddHistoryItem(GameShoppingHistoryItemV2 item, GameChar gameChar)
        {
            SaveHistoryItem(item);
            gameChar.ShoppingHistoryV2.Add(item);
        }

        /// <summary>
        /// 追加集合，并保存到数据库。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="gameChar"></param>
        public void AddHistoryItems(IEnumerable<GameShoppingHistoryItemV2> items, GameChar gameChar)
        {
            SaveHistoryItems(items);
            gameChar.ShoppingHistoryV2.AddRange(items);
        }

        #endregion 商品购买历史记录相关

        #region 自转周期相关

        /// <summary>
        /// 指定商品的自转周期是否已经发生变化。
        /// 首次计算则一律视为已经变化
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public bool IsChanged(GameChar gameChar, Guid shoppingTid, out int? periodIndex)
        {
            periodIndex = null;
            if (GetShoppingTemplateByTId(shoppingTid) is not TemplateStringFullView tt) return false;
            periodIndex = _SearcherManager.GetPeriodIndex(tt.ShoppingItem.Ins, gameChar, out var entity);
            var history = gameChar.PeriodIndexHistory.FirstOrDefault(c => c.TId == shoppingTid);
            if (history is null)    //若未计算
            {
                history = new GameShoppingHistoryItem
                {
                    TId = shoppingTid,
                    PeriodIndex = periodIndex,
                };
                gameChar.PeriodIndexHistory.Add(history);
                return true;
            }
            if (history.PeriodIndex == periodIndex) return false;
            history.PeriodIndex = periodIndex;
            return true;
        }

        /// <summary>
        /// 指定页签下所有商品，有任何一个周期变化即认为变化了。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="genus"></param>
        /// <returns>true有商品的周期发生了变化；否则为false。</returns>
        public bool IsChanged(GameChar gameChar, string genus)
        {
            var templates = _TemplateManager.GetTemplatesFromGenus("gs_jinzhu");
            bool changed = false;
            foreach (var template in templates)
            {
                if (GetShoppingItemByTemplate(template) is not GameShoppingItem) continue;
                if (IsChanged(gameChar, template.TemplateId, out _)) changed = true;
            }
            return changed;
        }

        /// <summary>
        /// 若金猪商品变化了需要做的处理。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="changes"></param>
        public void JinzhuChanged(GameChar gameChar, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            /*311bae23-09b4-4d8b-b158-f3129d5f6503	金猪累计金币占位符
            a84bcbd1-9541-4907-99df-59b19559ae9f	金猪累计钻石占位符*/
            Guid[] tids = new Guid[] { Guid.Parse("311bae23-09b4-4d8b-b158-f3129d5f6503"), Guid.Parse("a84bcbd1-9541-4907-99df-59b19559ae9f") };
            var coll = _EntityManager.GetAllEntity(gameChar).Where(c => tids.Contains(c.TemplateId)).Select(c => new GameEntitySummary
            {
                TId = c.TemplateId,
                Count = -c.Count,
            }).ToArray();
            _EntityManager.CreateAndMove(coll, gameChar, changes);
        }

        /// <summary>
        /// 若礼包商品周期变化了，调用此函数进行相应处理。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="changes"></param>
        public void LibaoChanged(GameChar gameChar, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            /*cb3eb8b7-15b5-4480-a56b-24bf6fca9a12	礼包商店充值次数占位符
            */
            Guid[] tids = new Guid[] { Guid.Parse("cb3eb8b7-15b5-4480-a56b-24bf6fca9a12") };
            var coll = _EntityManager.GetAllEntity(gameChar).Where(c => tids.Contains(c.TemplateId)).Select(c => new GameEntitySummary
            {
                TId = c.TemplateId,
                Count = -c.Count,
            }).ToArray();
            _EntityManager.CreateAndMove(coll, gameChar, changes);
        }

        /// <summary>
        /// 刷新金猪占位符的Count值。
        /// </summary>
        /// <param name="gc">角色。</param>
        /// <param name="worldUtc">世界时间。</param>
        public void InitJinzhu(GameChar gc, DateTime worldUtc)
        {
            if (_EntityManager.GetAllEntity(gc).FirstOrDefault(c => c.TemplateId == Guid.Parse("9f9fff74-7426-4022-8e3e-06cb93a1806c")) is GameEntity jinzhu)
            {
                jinzhu.Count = (worldUtc.Date - jinzhu.CreateDateTime.Value.Date).Days;
            }

        }
        #endregion 自转周期相关

        #region 获取信息

        /// <summary>
        /// 获取代表商品项的模板。
        /// </summary>
        /// <param name="shoppingItemTId"></param>
        /// <returns></returns>
        public TemplateStringFullView GetShoppingTemplateByTId(Guid shoppingItemTId)
        {
            var result = _TemplateManager.GetFullViewFromId(shoppingItemTId);
            if (result is null || GetShoppingItemByTemplate(result) is null)
                return null;
            return result;
        }

        /// <summary>
        /// 获取商品项数据。
        /// </summary>
        /// <param name="shoppingItemTId"></param>
        /// <returns></returns>
        public GameShoppingItem GetShoppingItemByTId(Guid shoppingItemTId)
        {
            var tt = _TemplateManager.GetFullViewFromId(shoppingItemTId);
            if (tt is null) return null;
            return GetShoppingItemByTemplate(tt);
        }

        /// <summary>
        /// 获取商品信息。
        /// </summary>
        /// <param name="tt"></param>
        /// <returns></returns>
        public GameShoppingItem GetShoppingItemByTemplate(TemplateStringFullView tt)
        {
            if (tt.ShoppingItem is not GameShoppingItem shoppingItem)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"指定的模板不包含商品项信息。TId={tt.TemplateId}");
                return null;
            }
            return shoppingItem;
        }

        /// <summary>
        /// 编码字符串为不可读形态。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string EncodeString(string str)
        {
            var ary = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(ary);
        }

        /// <summary>
        /// 从不可读形态获取可读形态字符串。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string DecodeString(string str)
        {
            try
            {
                var ary = Convert.FromBase64String(str);
                return Encoding.UTF8.GetString(ary);
            }
            catch (Exception)
            {
                return null;
            }
        }
        #endregion 获取信息

        /// <summary>
        /// 综合考虑多种因素确定是否可以购买。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="tt"></param>
        /// <param name="nowUtc"></param>
        /// <param name="periodStart"></param>
        /// <returns></returns>
        public bool IsMatch(GameChar gameChar, TemplateStringFullView tt, DateTime nowUtc, out DateTime periodStart)
        {
            if (tt.ShoppingItem is not GameShoppingItem shoppingItem)
            {
                periodStart = default;
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"指定的模板不包含商品项信息。");
                return false;
            }
            if (!IsMatchWithoutBuyed(gameChar, shoppingItem, nowUtc, out periodStart, 1)) return false;  //若时间点无效
            var end = periodStart + shoppingItem.Period.ValidPeriod;
            if (!IsMatchOnlyCount(gameChar, tt, periodStart, end, out _))
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_NOT_ENOUGH_QUOTA);
                OwHelper.SetLastErrorMessage($"已达最大购买数量。");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 测试角色是否可以购买指定的商品项。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="tt"></param>
        /// <param name="nowUtc"></param>
        /// <param name="periodStart">返回true时这里返回<paramref name="nowUtc"/>时间点所处周期的起始时间点。其它情况此值是随机值。</param>
        /// <param name="mask">条件组掩码。</param>
        /// <returns>true指定的商品项对指定用户而言在指定时间点上有效。</returns>
        public bool IsMatchWithoutBuyed(GameChar gameChar, TemplateStringFullView tt, DateTime nowUtc, out DateTime periodStart, int mask)
        {
            //if (tt.DisplayName.Contains("第1组-"))
            //    ;
            var shoppingItem = GetShoppingItemByTemplate(tt);
            if (shoppingItem is null)
            {
                periodStart = default;
                return false;
            }
            return IsMatchWithoutBuyed(gameChar, shoppingItem, OwHelper.WorldNow, out periodStart, mask);
        }

        /// <summary>
        /// 指定的商品项对指定角色是否有效，不考虑已经购买的数量。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="shoppingItem"></param>
        /// <param name="nowUtc"></param>
        /// <param name="periodStart"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public bool IsMatchWithoutBuyed(GameChar gameChar, GameShoppingItem shoppingItem, DateTime nowUtc, out DateTime periodStart, int mask)
        {
            if (!shoppingItem.Period.IsValid(nowUtc, out periodStart)) return false;  //若时间点无效
                                                                                      //检测购买代价
                                                                                      //if (!shoppingItem.Ins.All(c => c.Conditional.All(d => d.IsValidate(mask))))
                                                                                      //    return false;
            var entities = _EntityManager.GetAllEntity(gameChar);
            var ins = shoppingItem.Ins.Select(c => _BlueprintManager.Transformed(c, entities));
            var b = _SearcherManager.GetMatches(entities, ins, mask);
            if (b.Any(c => c.Item1 is null))
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"找不到符合条件的实体。");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 指定商品项是否可以购买，仅考虑已购买数量，不考虑其它因素。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="tt"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="buyedCount">在指定时间段内已经购买的数量。</param>
        /// <returns></returns>
        public bool IsMatchOnlyCount(GameChar gameChar, TemplateStringFullView tt, DateTime start, DateTime end, out decimal buyedCount)
        {
            var periodIndex = _SearcherManager.GetPeriodIndex(tt.ShoppingItem.Ins, gameChar, out _); //获取自周期数
            var collLoggin = gameChar.ShoppingHistoryV2;

            if (periodIndex.HasValue) //若存在自周期
            {
                var tmp = collLoggin?.Where(c => c.TId == tt.TemplateId)
                    .Where(c => c.PeriodIndex == periodIndex).Sum(c => c.Count) ?? decimal.Zero;
                if (tmp >= tt.ShoppingItem.MaxCount)
                {
                    buyedCount = tmp;
                    return false;
                }
                buyedCount = tmp;
                return true;
            }
            buyedCount = collLoggin?.Where(c => c.WorldDateTime >= start && c.WorldDateTime < end && c.TId == tt.TemplateId).Sum(c => c.Count) ?? decimal.Zero;  //已经购买的数量
            return buyedCount < (tt.ShoppingItem.MaxCount ?? decimal.MaxValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="tt"></param>
        /// <param name="nowUtc"></param>
        /// <param name="changes"></param>
        public bool Buy(GameChar gameChar, TemplateStringFullView tt, DateTime nowUtc, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            if (!IsMatch(gameChar, tt, nowUtc, out _)) return false;  //若不符合购买条件

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="tt"></param>
        /// <param name="nowUtc"></param>
        /// <returns>(当前周期起始时间点,本周期内已买次数,是否有效)，若无效<see cref="OwHelper.GetLastError"/>不是<see cref="ErrorCodes.NO_ERROR"/>则说明有错。</returns>
        public (DateTime, decimal, bool) GetShoppingItemState(GameChar gameChar, GameShoppingItem tt, DateTime nowUtc)
        {
            return default;
        }

        #region 法币购买相关

        #endregion 法币购买相关
    }
}
