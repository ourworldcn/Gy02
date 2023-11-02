using GY02.Managers;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * 包含各种未分类成就。
 */

namespace GY02.Commands
{
    /// <summary>
    /// 计算合成次数。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<CompositeCommand>))]
    public class AchievementCompositedHandler : ISyncCommandHandled<CompositeCommand>
    {
        public AchievementCompositedHandler(GameAchievementManager achievementManager, GameEventManager eventManager)
        {
            _AchievementManager = achievementManager;
            _EventManager = eventManager;
        }

        GameAchievementManager _AchievementManager;
        GameEventManager _EventManager;

        public void Handled(CompositeCommand command, Exception exception)
        {
            if (command.HasError || exception is not null) return;

            var now = OwHelper.WorldNow;
            SimpleGameContext context = new SimpleGameContext(Guid.Empty, command.GameChar, now, null);
            //8b948fa5-5a0d-44b9-a4de-1dcced307dee	合成装备次数变化事件
            _EventManager.SendEvent(Guid.Parse("8b948fa5-5a0d-44b9-a4de-1dcced307dee"), 1, context);
        }
    }

    /// <summary>
    /// 累计孵化次数。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<FuhuaCommand>))]
    public class AchievementFuhuaedHandler : ISyncCommandHandled<FuhuaCommand>
    {
        public AchievementFuhuaedHandler(GameAchievementManager achievementManager, GameEventManager eventManager)
        {
            _AchievementManager = achievementManager;
            _EventManager = eventManager;
        }

        GameAchievementManager _AchievementManager;
        GameEventManager _EventManager;

        public void Handled(FuhuaCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            SimpleGameContext context = new SimpleGameContext(Guid.Empty, command.GameChar, now, null);
            //156d8052-ded1-42a4-95b0-63d40d9fc52c	坐骑孵化次数变化事件
            _EventManager.SendEvent(Guid.Parse("156d8052-ded1-42a4-95b0-63d40d9fc52c"), 1, context);
        }
    }

    /// <summary>
    /// 升级装备次数变化事件。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<LvUpCommand>))]
    public class LvUped : ISyncCommandHandled<LvUpCommand>
    {
        public LvUped(GameAchievementManager achievementManager, GameEventManager eventManager)
        {
            _AchievementManager = achievementManager;
            _EventManager = eventManager;
        }

        GameAchievementManager _AchievementManager;
        GameEventManager _EventManager;

        public void Handled(LvUpCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            SimpleGameContext context = new SimpleGameContext(Guid.Empty, command.GameChar, now, null);

            //b13aa107-0134-492c-bb8d-c3c5855954c5	升级装备次数变化事件
            if (command.Ids.Count > 0)
                _EventManager.SendEvent(Guid.Parse("b13aa107-0134-492c-bb8d-c3c5855954c5"), command.Ids.Count, context);
        }
    }

    /// <summary>
    /// 04382674-0309-432c-b400-1bc80955eff2	开宝箱次数变化事件
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<ShoppingBuyCommand>))]
    public class ChoujiangBaoxiangClass : ISyncCommandHandled<ShoppingBuyCommand>
    {

        public ChoujiangBaoxiangClass(GameAchievementManager achievementManager, GameShoppingManager shoppingManager, GameEventManager eventManager)
        {
            _AchievementManager = achievementManager;
            _ShoppingManager = shoppingManager;
            _EventManager = eventManager;
        }

        GameAchievementManager _AchievementManager;
        GameShoppingManager _ShoppingManager;
        GameEventManager _EventManager;

        public void Handled(ShoppingBuyCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            SimpleGameContext context = new SimpleGameContext(Guid.Empty, command.GameChar, now, null);
            if (_ShoppingManager.GetShoppingTemplateByTId(command.ShoppingItemTId) is TemplateStringFullView tt)
            {
                if (tt.Genus?.Contains("gs_choujiangbaoxiang") ?? false)    //若是宝箱
                {
                    // 5d809337-9782-457d-a71d-8c6f1ac0e976	开宝箱次数变化事件
                    _EventManager.SendEvent(Guid.Parse("5d809337-9782-457d-a71d-8c6f1ac0e976"), command.Count, context);
                }
                string[] tmpGenus = new string[] { "gs_jibigoumai", "gs_zs", "gs_meirishangdian" };
                if (tt.Genus is not null && tmpGenus.Intersect(tt.Genus).Any())
                {
                    //d23ee35e-2e49-402b-83a3-effc4d8ff7aa	商店中购买物品次数变化事件
                    _EventManager.SendEvent(Guid.Parse("d23ee35e-2e49-402b-83a3-effc4d8ff7aa"), command.Count, context);
                }
            }
        }
    }

}
