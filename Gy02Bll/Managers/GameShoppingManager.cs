using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public GameShoppingManager(IOptions<GameShoppingManagerOptions> options, ILogger<GameShoppingManager> logger, BlueprintManager blueprintManager, GameEntityManager entityManager, TemplateManager templateManager) : base(options, logger)
        {
            _BlueprintManager = blueprintManager;
            _EntityManager = entityManager;
            _TemplateManager = templateManager;
        }

        BlueprintManager _BlueprintManager;
        GameEntityManager _EntityManager;
        TemplateManager _TemplateManager;

        /// <summary>
        /// 获取代表商品项的模板。
        /// </summary>
        /// <param name="shoppingItemTId"></param>
        /// <returns></returns>
        public TemplateStringFullView GetShoppingTemplateByTId(Guid shoppingItemTId)
        {
            var result = _TemplateManager.GetFullViewFromId(shoppingItemTId);
            if (GetShoppingItemByTemplate(result) is null)
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

        public GameShoppingItem GetShoppingItemByTemplate(TemplateStringFullView tt)
        {
            if (tt.ShoppingItem is not GameShoppingItem shoppingItem)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"指定的模板不包含商品项信息。");
                return null;
            }
            return shoppingItem;
        }

        /// <summary>
        /// 综合考虑多种因素确定是否可以购买。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="tt"></param>
        /// <param name="nowUtc"></param>
        /// <param name="periodStart"></param>
        /// <returns></returns>
        public bool IsValid(GameChar gameChar, TemplateStringFullView tt, DateTime nowUtc, out DateTime periodStart)
        {
            if (!(tt.ShoppingItem is GameShoppingItem shoppingItem))
            {
                periodStart = default;
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"指定的模板不包含商品项信息。");
                return false;
            }
            if (!IsValidWithoutBuyed(gameChar, shoppingItem, nowUtc, out periodStart)) return false;  //若时间点无效
            var end = periodStart + shoppingItem.Period.ValidPeriod;
            if (!IsValidOnlyCount(gameChar, tt, periodStart, end, out _))
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
        /// <param name="ignore">是否忽略仅用于购买的条件。</param>
        /// <returns>true指定的商品项对指定用户而言在指定时间点上有效。</returns>
        public bool IsValidWithoutBuyed(GameChar gameChar, TemplateStringFullView tt, DateTime nowUtc, out DateTime periodStart, bool ignore = false)
        {
            if (tt.ShoppingItem is not GameShoppingItem shoppingItem)
            {
                periodStart = default;
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"指定的模板不包含商品项信息。");
                return false;
            }
            return IsValidWithoutBuyed(gameChar, shoppingItem, DateTime.UtcNow, out periodStart, ignore);
        }

        /// <summary>
        /// 指定的商品项对指定角色是否有效，不考虑已经购买的数量。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="shoppingItem"></param>
        /// <param name="nowUtc"></param>
        /// <param name="periodStart"></param>
        /// <returns></returns>
        public bool IsValidWithoutBuyed(GameChar gameChar, GameShoppingItem shoppingItem, DateTime nowUtc, out DateTime periodStart, bool ignore = false)
        {
            if (!shoppingItem.Period.IsValid(nowUtc, out periodStart)) return false;  //若时间点无效
            //检测购买代价
            if (ignore && shoppingItem.Ins.All(c => c.IgnoreIfDisplayList))
                return true;
            var costs = _BlueprintManager.GetCost(gameChar.GetAllChildren().Select(c => _EntityManager.GetEntity(c)), shoppingItem.Ins);
            if (costs is null)
                return false;
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
        public bool IsValidOnlyCount(GameChar gameChar, TemplateStringFullView tt, DateTime start, DateTime end, out decimal buyedCount)
        {
            buyedCount = gameChar.ShoppingHistory?.Where(c => c.DateTime >= start && c.DateTime < end && c.TId == tt.TemplateId).Sum(c => c.Count) ?? decimal.Zero;  //已经购买的数量
            return buyedCount <= tt.ShoppingItem.MaxCount;
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
            if (!IsValid(gameChar, tt, nowUtc, out _)) return false;  //若不符合购买条件

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
    }
}
