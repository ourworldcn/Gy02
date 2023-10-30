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
        public AchievementShoppingBuyedHandler(GameAchievementManager achievementManager, GameEventManager eventManager)
        {
            _AchievementManager = achievementManager;
            _EventManager = eventManager;
        }

        GameAchievementManager _AchievementManager;
        GameEventManager _EventManager;

        public void Handled(ShoppingBuyCommand command, Exception exception)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            SimpleGameContext context = new SimpleGameContext(Guid.Empty, command.GameChar, now, null);
            //822b1d80-70fe-417d-baea-e9c2aacbdcd8	购买体力的数量成就
            //体力商品的TId
            var tiliGoodsTId = new Guid[] { Guid.Parse("59bf4cb7-5bc2-48ee-98ad-2efbf116b162"), Guid.Parse("622b4572-782d-4bee-b4c5-2d512daa3714") };
            if (tiliGoodsTId.Contains(command.ShoppingItemTId)) //可能需要处理购买体力的成就
            {
                _EventManager.SendEvent(Guid.Parse("f0aeedb4-890b-48ce-96e3-964dad23beb4"), 5 * command.Count, context);

                //var inc = command.Changes.Where(c =>  //获取体力增量
                //{
                //    if (c.Object is not GameEntity entity) return false;
                //    if (entity.TemplateId != ProjectContent.PowerTId) return false;
                //    return true;
                //}).Select(c => (c.HasOldValue && OwConvert.TryToDecimal(c.OldValue, out var ov) ? ov : 0m, c.HasNewValue && OwConvert.TryToDecimal(c.NewValue, out var nv) ? nv : 0))
                //.Sum(c => c.Item2 - c.Item1);
            }
        }
    }

    /// <summary>
    /// 60be1c6e-e144-4109-9684-b2038df7ee2b	使用快速巡逻次数成就
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<ShoppingBuyCommand>))]
    public class KuaisuXunluoBuyedHandler : ISyncCommandHandled<ShoppingBuyCommand>
    {
        public KuaisuXunluoBuyedHandler(GameAchievementManager achievementManager, GameEventManager eventManager)
        {
            _AchievementManager = achievementManager;
            _EventManager = eventManager;
        }

        GameAchievementManager _AchievementManager;
        GameEventManager _EventManager;

        public void Handled(ShoppingBuyCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            SimpleGameContext context = new SimpleGameContext(Guid.Empty, command.GameChar, now, null);
            //41fb793d-43cf-4dc6-8fdc-526142058d6a	领取快速巡逻收益次数变化事件
            var tids = new Guid[] { Guid.Parse("89a66863-4d5e-48f6-b8e6-4f2b949239af"), Guid.Parse("f7ad9c3c-c9d1-4773-9eb5-f77bf7b4162a") };//89a66863-4d5e-48f6-b8e6-4f2b949239af	快速巡逻购买
            if (tids.Contains(command.ShoppingItemTId)) //若是快速巡逻
            {
                _EventManager.SendEvent(Guid.Parse("41fb793d-43cf-4dc6-8fdc-526142058d6a"), command.Count, context);
            }
            //1d715f99-9b38-4241-ae95-f6335cea5e6a	领取一般巡逻收益次数变化事件
            if (command.ShoppingItemTId == Guid.Parse("16a21a09-c9c2-48cb-8f91-40b4d82f3477"))    //16a21a09-c9c2-48cb-8f91-40b4d82f3477	关卡巡逻购买
                _EventManager.SendEvent(Guid.Parse("1d715f99-9b38-4241-ae95-f6335cea5e6a"), command.Count, context);
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
        public ZuanshiXiaohaoHandler(GameAchievementManager achievementManager, GameEventManager eventManager)
        {
            _AchievementManager = achievementManager;
            _EventManager = eventManager;
        }

        GameAchievementManager _AchievementManager;
        GameEventManager _EventManager;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="command"></param>
        /// <param name="exception"><inheritdoc/></param>
        public void Handled(ShoppingBuyCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            SimpleGameContext context = new SimpleGameContext(Guid.Empty, command.GameChar, now, null);

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
            //94aabc9c-79da-475b-af43-b837246a681e	消耗钻石数量变化事件
            if (inc > 0)
                _EventManager.SendEvent(Guid.Parse("94aabc9c-79da-475b-af43-b837246a681e"), inc, context);
            //7f6b561a-705c-47ba-9765-22e41e1ce1d9	消耗金币数量变化事件
            coll = command.Changes.Where(c =>
           {
               if (c.Object is not GameEntity entity) return false;
               if (entity.TemplateId != ProjectContent.GoldTId) return false;
               if (!c.HasOldValue || !c.HasNewValue) return false;
               return true;
           });
            inc = coll.Sum(c =>
           {
               if (!OwConvert.TryToDecimal(c.OldValue, out var ov) || !OwConvert.TryToDecimal(c.NewValue, out var nv))
                   return 0;
               return Math.Max(0, ov - nv);
           });
            if (inc > 0)
                _EventManager.SendEvent(Guid.Parse("7f6b561a-705c-47ba-9765-22e41e1ce1d9"), inc, context);
        }
    }
}

