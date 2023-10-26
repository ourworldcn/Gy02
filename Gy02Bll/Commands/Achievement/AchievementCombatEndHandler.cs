/*
 * 副本结算相关成就。
 */

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

namespace GY02.Commands.Achievement
{
    /// <summary>
    /// 统计杀怪成就的处理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<EndCombatCommand>))]
    public class AchievementCombatEndHandler : ISyncCommandHandled<EndCombatCommand>
    {
        public AchievementCombatEndHandler(GameTemplateManager templateManager, GameAchievementManager achievementManager, GameCombatManager combatManager)
        {
            _TemplateManager = templateManager;
            _AchievementManager = achievementManager;
            _CombatManager = combatManager;
        }

        GameTemplateManager _TemplateManager;
        GameAchievementManager _AchievementManager;
        GameCombatManager _CombatManager;

        public void Handled(EndCombatCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;   //忽略错误的结算信息
            var now = OwHelper.WorldNow;
            if (_CombatManager.GetTemplateById(command.CombatTId) is TemplateStringFullView ttCombat)
            {
                if (ttCombat.Gid / 1000 == 210101)   //若是主线任务关卡
                {
                    var coll = from summary in command.Others
                               let tt = _TemplateManager.GetFullViewFromId(summary.TId)
                               where tt?.Genus is not null
                               group summary by tt into g
                               select (tt: g.Key, count: g.Sum(c => c.Count));
                    #region 杀怪数量 types_jingying types_putong types_all types_boss types_egg
                    var types_all = coll.Where(c => c.tt.Genus.Contains("types_all")).Sum(c => c.count);
                    var types_putong = coll.Where(c => c.tt.Genus.Contains("types_putong")).Sum(c => c.count);
                    var types_jingying = coll.Where(c => c.tt.Genus.Contains("types_jingying")).Sum(c => c.count);
                    var types_boss = coll.Where(c => c.tt.Genus.Contains("types_boss")).Sum(c => c.count);
                    var types_egg = coll.Where(c => c.tt.Genus.Contains("types_egg")).Sum(c => c.count);

                    //杀死怪物数量成就
                    var ttAchi = _AchievementManager.GetTemplateById(new Guid("e4947911-e113-47ba-b28b-62d1ac441ab8"));
                    var achievement = _AchievementManager.GetOrCreate(command.GameChar, ttAchi);
                    _AchievementManager.RaiseEventIfChanged(achievement, types_all, command.GameChar, now);
                    //杀死精英怪物数量成就
                    ttAchi = _AchievementManager.GetTemplateById(new Guid("ad102f96-b971-460b-a894-9fde078fee4d"));
                    achievement = _AchievementManager.GetOrCreate(command.GameChar, ttAchi);
                    _AchievementManager.RaiseEventIfChanged(achievement, types_jingying, command.GameChar, now);
                    //杀死Boss怪物数量成就
                    ttAchi = _AchievementManager.GetTemplateById(new Guid("21889fad-e13e-4b8a-b580-f31274aa9d65"));
                    achievement = _AchievementManager.GetOrCreate(command.GameChar, ttAchi);
                    _AchievementManager.RaiseEventIfChanged(achievement, types_boss, command.GameChar, now);
                    //8d1ea12f-26be-4fe4-acbe-ad1c7d053131	关卡中的打蛋数量成就
                    ttAchi = _AchievementManager.GetTemplateById(new Guid("8d1ea12f-26be-4fe4-acbe-ad1c7d053131"));
                    achievement = _AchievementManager.GetOrCreate(command.GameChar, ttAchi);
                    _AchievementManager.RaiseEventIfChanged(achievement, types_egg, command.GameChar, now);
                    #endregion 杀怪数量 
                    //2913b8e2-3db3-4204-b36c-415d6bc6b3f0	闯关数量成就
                    _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("2913b8e2-3db3-4204-b36c-415d6bc6b3f0"), 1, command.GameChar, now);
                    //cj_guanqia 单个关卡通关成就
                    if (command.IsSuccess)
                    {
                        var achiTt = _AchievementManager.GetTemplateByGenus("cj_guanqia", command.CombatTId);
                        if (achiTt is not null && _AchievementManager.GetOrCreate(command.GameChar, achiTt) is GameAchievement achi && achi.Count < achiTt.Achievement.Exp2LvSequence[^1])
                        {
                            _AchievementManager.RaiseEventIfChanged(achi, 1, command.GameChar, now);
                        }
                    }
                    //b8e00fd4-df86-4570-952a-8bd3c0435fd2	每日任务-子任务1（杀死主线关卡怪物数量）
                    _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("b8e00fd4-df86-4570-952a-8bd3c0435fd2"), types_all, command.GameChar, now);
                }
            }
        }
    }

    /// <summary>
    /// 主线副本通关成就记录事件。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<EndCombatCommand>))]
    public class ZhuxianFubenTongguanHandler : ISyncCommandHandled<EndCombatCommand>
    {
        public ZhuxianFubenTongguanHandler(GameAchievementManager achievementManager, GameCombatManager combatManager, GameTemplateManager templateManager)
        {
            _AchievementManager = achievementManager;
            _CombatManager = combatManager;
            _TemplateManager = templateManager;
        }

        GameAchievementManager _AchievementManager;
        GameCombatManager _CombatManager;
        GameTemplateManager _TemplateManager;

        public void Handled(EndCombatCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            var tt = _CombatManager.GetTemplateById(command.CombatTId);
            if (tt is null) goto lbErr;
            //43E9286A-904C-4923-B477-482C0D6470A5	主线副本通关进度,必须成功
            if (command.IsSuccess && tt.Gid is int gid && gid / 1000 == 210101)   //若不是主线任务副本
            {
                var count = gid % 1000; //通关经验值
                _AchievementManager.RaiseEventIfSetAndChanged(new Guid("43E9286A-904C-4923-B477-482C0D6470A5"), count, command.GameChar, now);
            }

            // 1fbe6bb9-84be-4098-8430-e7c46a6135f1	开服活动成就- 累计杀怪数量
            var tts = _TemplateManager.GetTemplatesFromGenus("types_all");  //符合要求怪的模板集合
            var tids = new HashSet<Guid>(tts.Select(c => c.TemplateId));    //符合要求怪的模板Id集合
            var inc = command.Others.Where(c => tids.Contains(c.TId)).Sum(c => c.Count);    //增加的杀怪数量
            _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("1fbe6bb9-84be-4098-8430-e7c46a6135f1"), inc, command.GameChar, now);
            return;
        lbErr:
            command.FillErrorFromWorld();
            return;
        }
    }

    /// <summary>
    /// 86833e6b-81bf-47ca-9965-b57c2012ecfd	开服活动成就-累计挑战关卡次数
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ISyncCommandHandled<StartCombatCommand>))]
    public class CombatStartHandler : ISyncCommandHandled<StartCombatCommand>
    {
        public CombatStartHandler(GameAchievementManager achievementManager, GameCombatManager combatManager)
        {
            _AchievementManager = achievementManager;
            _CombatManager = combatManager;
        }

        GameAchievementManager _AchievementManager;
        GameCombatManager _CombatManager;

        public void Handled(StartCombatCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            if (_CombatManager.GetTemplateById(command.CombatTId) is TemplateStringFullView tt && tt.Gid is int gid && gid / 1000 == 210101)    //主线关卡
                _AchievementManager.RaiseEventIfIncreaseAndChanged(Guid.Parse("86833e6b-81bf-47ca-9965-b57c2012ecfd"), 1, command.GameChar, now);
        }
    }
}
