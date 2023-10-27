using GY02.Managers;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Entity;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands.Shopping
{
    /// <summary>
    /// 增加赛季VIP开启功能。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<ShoppingBuyCommand>))]
    public class Saiji : ISyncCommandHandled<ShoppingBuyCommand>
    {

        public Saiji(GameAchievementManager achievementManager)
        {
            _AchievementManager = achievementManager;
        }

        GameAchievementManager _AchievementManager;

        public void Handled(ShoppingBuyCommand command, Exception exception = null)
        {
            if (command.Changes.Count <= 0 && (command.HasError || exception is not null)) return;
            var tid = Guid.Parse("c9615e53-ff8c-4080-ab20-ec8c5d170a3b");
            var coll1 = command.Changes.Where(c => (c.Object as GameEntity)?.TemplateId == tid).Select(c => c.Object as GameEntity);
            var coll2 = command.Changes.Where(c => (c.NewValue as GameEntity)?.TemplateId == tid).Select(c => c.NewValue as GameEntity);
            if (coll1.Concat(coll2).Any(c => c.Count > 0))   //若开启赛季vip门票
            {
                var fee = _AchievementManager.GetOrCreate(command.GameChar, Guid.Parse("008d96c5-9545-44fb-965d-6d1c9d97f97d"));    //008d96c5-9545-44fb-965d-6d1c9d97f97d	赛季免费门票成就
                var vip = _AchievementManager.GetOrCreate(command.GameChar, Guid.Parse("64aa13e6-70bc-4fe4-a112-09dcb2160f35"));    //64aa13e6-70bc-4fe4-a112-09dcb2160f35	赛季VIP1门票成就
                if (vip.Count != fee.Count)
                    _AchievementManager.SetExperience(vip, fee.Count, new SimpleGameContext(Guid.Empty, command.GameChar, OwHelper.WorldNow, command.Changes));
                else if (vip.Count == 0)
                    _AchievementManager.InitializeState(vip);
            }
        }
    }
}
