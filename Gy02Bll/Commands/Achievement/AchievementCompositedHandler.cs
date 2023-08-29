using GY02.Managers;
using Microsoft.Extensions.DependencyInjection;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (!_AchievementManager.RaiseEventIfLevelChanged(Guid.Parse("4f6a92f2-3aac-479b-92e8-0dfffa74536b"), 1, command.GameChar, OwHelper.WorldNow))
                command.FillErrorFromWorld();
        }
    }
}
