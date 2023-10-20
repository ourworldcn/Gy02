/*
 * 日任务和周任务推进
 * 
 */

using GY02.Commands;
using GY02.Managers;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using OW.SyncCommand;

namespace GY02
{
    /// <summary>
    /// 副本开始。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<StartCombatCommand>))]
    public class DayTask1 : ISyncCommandHandled<StartCombatCommand>
    {
        public DayTask1(GameAchievementManager achievementManager, GameCombatManager combatManager)
        {
            AchievementManager = achievementManager;
            CombatManager = combatManager;
        }

        GameAchievementManager AchievementManager { get; set; }
        GameCombatManager CombatManager { get; set; }

        public void Handled(StartCombatCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;   //忽略错误的结算信息
            var now = OwHelper.WorldNow;
            if (CombatManager.GetTemplateById(command.CombatTId) is TemplateStringFullView ttCombat)
            {
                if (ttCombat.Gid / 1000 == 210101)   //若是主线任务关卡
                {
                    AchievementManager.RaiseEventIfChanged(Guid.Parse("9ee462a9-36a6-4199-8169-ad64e83c3ccc"), 1, command.GameChar, now);
                    //5f91380c-961c-4311-a51a-57ad73222045	每日任务-子任务2（主线关卡挑战数量）
                    AchievementManager.RaiseEventIfChanged(Guid.Parse("5f91380c-961c-4311-a51a-57ad73222045"), 1, command.GameChar, now);
                    //d6448445-f7da-42e9-a383-36f63731de6b	每日任务-子任务4（主线关卡挑战数量）
                    AchievementManager.RaiseEventIfChanged(Guid.Parse("d6448445-f7da-42e9-a383-36f63731de6b"), 1, command.GameChar, now);
                    //0d188f0d-0040-48ed-9ba1-b946413ea82b	每日任务-子任务5（主线关卡挑战数量）
                    AchievementManager.RaiseEventIfChanged(Guid.Parse("0d188f0d-0040-48ed-9ba1-b946413ea82b"), 1, command.GameChar, now);
                    //0913a1e2-dbd5-4b4c-a1f8-ca6c6680c794	每日任务-子任务6（主线关卡挑战数量）
                    AchievementManager.RaiseEventIfChanged(Guid.Parse("0913a1e2-dbd5-4b4c-a1f8-ca6c6680c794"), 1, command.GameChar, now);
                    //388ae832-9556-43a8-9165-c2d5ffc6b2f4	每日任务-子任务7（主线关卡挑战数量）
                    AchievementManager.RaiseEventIfChanged(Guid.Parse("388ae832-9556-43a8-9165-c2d5ffc6b2f4"), 1, command.GameChar, now);
                }
                //b8c3b51e-c53a-46c6-b587-b445d422c584	每日任务-子任务1（特殊关卡挑战1次）
                if (ttCombat.Gid / 100_000 == 2102)   //若是主线任务关卡
                {
                    AchievementManager.RaiseEventIfChanged(Guid.Parse("b8c3b51e-c53a-46c6-b587-b445d422c584"), 1, command.GameChar, now);
                }
            }

        }
    }

}