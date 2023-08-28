using GY02.Managers;
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
    public class AchievementCompositedHandler : SyncCommandHandlerBase<CombatEndCommand>
    {
        public AchievementCompositedHandler(GameAchievementManager achievementManager)
        {
            _AchievementManager = achievementManager;
        }

        GameAchievementManager _AchievementManager;

        public override void Handle(CombatEndCommand command)
        {
            if (!_AchievementManager.RaiseEventIfLevelChanged(Guid.Parse("4f6a92f2-3aac-479b-92e8-0dfffa74536b"), 1, command.GameChar, OwHelper.WorldNow))
                command.FillErrorFromWorld();
        }
    }
}
