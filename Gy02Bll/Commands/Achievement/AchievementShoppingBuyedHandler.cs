using GY02.Managers;
using GY02.Publisher;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.SyncCommand;
using System.Data;

namespace GY02.Commands
{
    /// <summary>
    /// 
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<ShoppingBuyCommand>))]
    public class AchievementShoppingBuyedHandler : ISyncCommandHandled<ShoppingBuyCommand>
    {
        public AchievementShoppingBuyedHandler(GameAchievementManager achievementManager)
        {
            _AchievementManager = achievementManager;
        }

        GameAchievementManager _AchievementManager;

        public void Handled(ShoppingBuyCommand command, Exception exception)
        {
            if (command.HasError || exception is not null) return;
            //822b1d80-70fe-417d-baea-e9c2aacbdcd8	购买体力的数量成就
            //体力商品的TId
            var tiliGoodsTId = new Guid[] { Guid.Parse("59bf4cb7-5bc2-48ee-98ad-2efbf116b162"), Guid.Parse("622b4572-782d-4bee-b4c5-2d512daa3714") };
            if (tiliGoodsTId.Contains(command.ShoppingItemTId)) //可能需要处理购买体力的成就
            {
                var now = OwHelper.WorldNow;
                //var inc = command.Changes.Where(c =>  //获取体力增量
                //{
                //    if (c.Object is not GameEntity entity) return false;
                //    if (entity.TemplateId != ProjectContent.PowerTId) return false;
                //    return true;
                //}).Select(c => (c.HasOldValue && OwConvert.TryToDecimal(c.OldValue, out var ov) ? ov : 0m, c.HasNewValue && OwConvert.TryToDecimal(c.NewValue, out var nv) ? nv : 0))
                //.Sum(c => c.Item2 - c.Item1);
                if (!_AchievementManager.RaiseEventIfChanged(Guid.Parse("822b1d80-70fe-417d-baea-e9c2aacbdcd8"), 1, command.GameChar, now)) command.FillErrorFromWorld();
            }
        }
    }

    /// <summary>
    /// 60be1c6e-e144-4109-9684-b2038df7ee2b	使用快速巡逻次数成就
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<ShoppingBuyCommand>))]
    public class KuaisuXunluoBuyedHandler : ISyncCommandHandled<ShoppingBuyCommand>
    {
        public KuaisuXunluoBuyedHandler(GameAchievementManager achievementManager)
        {
            _AchievementManager = achievementManager;
        }

        GameAchievementManager _AchievementManager;

        public void Handled(ShoppingBuyCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            // 89a66863-4d5e-48f6-b8e6-4f2b949239af	快速巡逻购买
            var tids = new Guid[] { Guid.Parse("89a66863-4d5e-48f6-b8e6-4f2b949239af"), Guid.Parse("f7ad9c3c-c9d1-4773-9eb5-f77bf7b4162a") };
            if (tids.Contains(command.ShoppingItemTId))
            {
                var achiTId = Guid.Parse("60be1c6e-e144-4109-9684-b2038df7ee2b");
                var now = OwHelper.WorldNow;
                _AchievementManager.RaiseEventIfChanged(achiTId, command.Count, command.GameChar, now);
                //2b773be5-a6fb-41e0-a8bd-e3c1ed61d150	每日任务-子任务1（领取快速巡逻收益）
                _AchievementManager.RaiseEventIfChanged(Guid.Parse("2b773be5-a6fb-41e0-a8bd-e3c1ed61d150"), command.Count, command.GameChar, now);
            }
        }
    }

    /// <summary>
    /// 663310b0-79d7-403c-9b03-078fd5155c9a	消耗钻石数量
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<ShoppingBuyCommand>))]
    public class ZuanshiXiaohaoHandler : ISyncCommandHandled<ShoppingBuyCommand>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="achievementManager"></param>
        public ZuanshiXiaohaoHandler(GameAchievementManager achievementManager)
        {
            _AchievementManager = achievementManager;
        }

        GameAchievementManager _AchievementManager;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="command"></param>
        /// <param name="exception"><inheritdoc/></param>
        public void Handled(ShoppingBuyCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;

            var coll = command.Changes.Where(c =>
            {
                if (c.Object is not GameEntity entity) return false;
                if (entity.TemplateId != ProjectContent.DiamTId) return false;
                if (!c.HasOldValue || !c.HasNewValue) return false;
                return true;
            });
            var inc = coll.Sum(c =>
            {
                if (!OwConvert.TryToDecimal(c.OldValue, out var ov) || !OwConvert.TryToDecimal(c.NewValue, out var nv))
                    return 0;
                return Math.Max(0, ov - nv);
            });
            if (inc > 0)
                _AchievementManager.RaiseEventIfChanged(Guid.Parse("663310b0-79d7-403c-9b03-078fd5155c9a"), inc, command.GameChar, now);
        }
    }
}

