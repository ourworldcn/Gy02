/*
 * 拦截各种事件转变为成就的代码。
 * 
 */

using GY02.Managers;
using GY02.Publisher;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Manager;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Commands
{
    /// <summary>
    /// 主线副本通关成就记录事件。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<EndCombatCommand>))]
    public class ZhuxianFubenTongguanHandler : ISyncCommandHandled<EndCombatCommand>
    {
        public ZhuxianFubenTongguanHandler(GameAchievementManager achievementManager, GameCombatManager combatManager)
        {
            _AchievementManager = achievementManager;
            _CombatManager = combatManager;
        }

        GameAchievementManager _AchievementManager;
        GameCombatManager _CombatManager;

        public void Handled(EndCombatCommand command, Exception exception = null)
        {
            var now = OwHelper.WorldNow;
            var tt = _CombatManager.GetTemplateById(command.CombatTId);
            if (tt is null) goto lbErr;
            if (!tt.Gid.HasValue)
            {
                command.DebugMessage = $"不是主线任务副本。";
                return;
            }
            var gid = tt.Gid.Value;
            if (gid / 1000 != 210101)
            {
                command.DebugMessage = $"不是主线任务副本。";
                return;
            }
            var count = gid % 1000; //通关经验值
            var ttAchi = _AchievementManager.GetTemplateById(new Guid("43E9286A-904C-4923-B477-482C0D6470A5"));
            if (ttAchi is null) goto lbErr;

            var achi = _AchievementManager.GetOrCreate(command.GameChar, ttAchi);
            if (achi is null) goto lbErr;
            if (achi.Count < count) //若完成了新成就。
            {
                var olvLv = achi.Level; //记录旧的等级
                achi.Count = count;
                _AchievementManager.RefreshState(achi, command.GameChar, now);
            }
            return;
        lbErr:
            command.FillErrorFromWorld();
            return;
        }
    }
}
