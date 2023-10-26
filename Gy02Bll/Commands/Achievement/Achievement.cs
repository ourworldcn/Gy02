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
        public AchievementCompositedHandler(GameAchievementManager achievementManager)
        {
            _AchievementManager = achievementManager;
        }

        GameAchievementManager _AchievementManager;

        public void Handled(CompositeCommand command, Exception exception)
        {
            if (command.HasError || exception is not null) return;
            //4f6a92f2-3aac-479b-92e8-0dfffa74536b	装备合成次数成就
            _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("4f6a92f2-3aac-479b-92e8-0dfffa74536b"), 1, command.GameChar, OwHelper.WorldNow);
            //db98951e-a908-482a-a2e4-0e851a3e1c95	开服活动成就- 累计合装备合成次数
            _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("db98951e-a908-482a-a2e4-0e851a3e1c95"), 1, command.GameChar, OwHelper.WorldNow);
        }
    }

    /// <summary>
    /// 累计孵化次数。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<FuhuaCommand>))]
    public class AchievementFuhuaedHandler : ISyncCommandHandled<FuhuaCommand>
    {
        public AchievementFuhuaedHandler(GameAchievementManager achievementManager)
        {
            _AchievementManager = achievementManager;
        }

        GameAchievementManager _AchievementManager;

        public void Handled(FuhuaCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            //22f48e2b-8e81-43fb-80dd-af900eb21a29	累计孵化次数成就
            _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("22f48e2b-8e81-43fb-80dd-af900eb21a29"), 1, command.GameChar, now);
            //c7772592-50ab-4f98-be01-b58c02571d46	开服活动成就- 累计孵化次数
            _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("c7772592-50ab-4f98-be01-b58c02571d46"), 1, command.GameChar, now);
            //401cbe25-ed70-4a43-a5a8-524cf57f56c2	每日任务-子任务1（坐骑孵化次数）
            _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("401cbe25-ed70-4a43-a5a8-524cf57f56c2"), 1, command.GameChar, now);
        }
    }

    /// <summary>
    /// e2c2cf15-263f-4341-acdd-c3135c73097b	开服活动成就 - 累计升级装备次数
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<LvUpCommand>))]
    public class LvUped : ISyncCommandHandled<LvUpCommand>
    {
        public LvUped(GameAchievementManager achievementManager)
        {
            _AchievementManager = achievementManager;
        }

        GameAchievementManager _AchievementManager;

        public void Handled(LvUpCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            if (command.Ids.Count > 0)
                _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("e2c2cf15-263f-4341-acdd-c3135c73097b"), command.Ids.Count, command.GameChar, now);
            //f7647e8e-cdf1-4a30-982e-fb676cfc6c34	每日任务-子任务1（升级装备次数）
            if (command.Ids.Count > 0)
                _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("f7647e8e-cdf1-4a30-982e-fb676cfc6c34"), command.Ids.Count, command.GameChar, now);

        }
    }

    /// <summary>
    /// 4963c720-2b8f-4def-aed2-7bc8925f6a91	开宝箱次数 gs_choujiangbaoxiang
    /// 5173f53e-6534-4594-82cc-d989f52f03c7	开服活动成就- 累计开宝箱次数
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<ShoppingBuyCommand>))]
    public class ChoujiangBaoxiangClass : ISyncCommandHandled<ShoppingBuyCommand>
    {

        public ChoujiangBaoxiangClass(GameAchievementManager achievementManager, GameShoppingManager shoppingManager)
        {
            _AchievementManager = achievementManager;
            _ShoppingManager = shoppingManager;
        }

        GameAchievementManager _AchievementManager;
        GameShoppingManager _ShoppingManager;

        public void Handled(ShoppingBuyCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            if (_ShoppingManager.GetShoppingTemplateByTId(command.ShoppingItemTId) is TemplateStringFullView tt)
            {
                if (tt.Genus?.Contains("gs_choujiangbaoxiang") ?? false)
                {
                    _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("4963c720-2b8f-4def-aed2-7bc8925f6a91"), command.Count, command.GameChar, OwHelper.WorldNow);
                    _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("5173f53e-6534-4594-82cc-d989f52f03c7"), command.Count, command.GameChar, OwHelper.WorldNow);
                    //f9ec4736-d926-43a6-98fb-44d1adb11faf	每日任务-子任务1（开宝箱次数）
                    _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("f9ec4736-d926-43a6-98fb-44d1adb11faf"), command.Count, command.GameChar, OwHelper.WorldNow);
                }
                //6020445b-ff60-4a8d-96b3-78e79f56fcf6	每日任务-子任务1（商店中购买物品次数）
                string[] tmpGenus = new string[] { "gs_jibigoumai", "gs_zs", "gs_meirishangdian" };
                if (tt.Genus is not null && tmpGenus.Intersect(tt.Genus).Any())
                {
                    _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("6020445b-ff60-4a8d-96b3-78e79f56fcf6"), command.Count, command.GameChar, OwHelper.WorldNow);
                }
            }
        }
    }

}
