using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
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
        public GameShoppingManager(IOptions<GameShoppingManagerOptions> options, ILogger<GameShoppingManager> logger, BlueprintManager blueprintManager) : base(options, logger)
        {
            _BlueprintManager = blueprintManager;
        }

        BlueprintManager _BlueprintManager;
        GameEntityManager _EntityManager;

        /// <summary>
        /// 测试角色是否可以购买指定的商品项。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="tt"></param>
        /// <param name="nowUtc"></param>
        /// <param name="periodStart">返回true时这里返回<paramref name="nowUtc"/>时间点所处周期的起始时间点。其它情况此值是随机值。</param>
        /// <returns>true指定的商品项对指定用户而言在指定时间点上有效。</returns>
        public bool IsValid(GameChar gameChar, TemplateStringFullView tt, DateTime nowUtc, out DateTime periodStart)
        {
            if (tt.ShoppingItem is not GameShoppingItem shoppingItem)
            {
                periodStart = default;
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"指定的模板不包含商品项信息。");
                return false;
            }
            periodStart = shoppingItem.Period.Start;
            if (nowUtc > shoppingItem.Period.End)   //若已经超期
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_INVALID_DATA);
                OwHelper.SetLastErrorMessage($"指定的时间{nowUtc}商品项最终有效期{shoppingItem.Period.End.Value}。");
                return false;
            }
            while (true)    //TODO 需要提高性能
            {
                if (periodStart > nowUtc) //若已经超期
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_INVALID_DATA);
                    OwHelper.SetLastErrorMessage($"指定的时间{nowUtc}不在商品有效期内。");
                    return false;
                }
                if (periodStart + shoppingItem.Period.ValidPeriod > nowUtc)    //若找到合适的项
                    break;
                periodStart += shoppingItem.Period.Period;
            }
            var end = periodStart + shoppingItem.Period.ValidPeriod;
            var start = periodStart;
            //校验购买数量
            var buyedCount = gameChar.ShoppingHistory.Where(c => c.DateTime >= start && c.DateTime < end).Sum(c => c.Count);  //已经购买的数量
            if (buyedCount >= shoppingItem.MaxCount)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_NOT_ENOUGH_QUOTA);
                OwHelper.SetLastErrorMessage($"已达最大购买数量。");
                return false;
            }
            //检测购买代价
            var costs = _BlueprintManager.GetCost(gameChar.GetAllChildren().Select(c => _EntityManager.GetEntity(c)), shoppingItem.Ins);
            if (costs is null)
                return false;
            return true;
        }
    }
}
