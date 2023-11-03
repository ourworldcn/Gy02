/*
 * 副本相关成就。
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
        public AchievementCombatEndHandler(GameTemplateManager templateManager, GameAchievementManager achievementManager, GameCombatManager combatManager, GameEventManager eventManager)
        {
            _TemplateManager = templateManager;
            _AchievementManager = achievementManager;
            _CombatManager = combatManager;
            _EventManager = eventManager;
        }

        GameTemplateManager _TemplateManager;
        GameAchievementManager _AchievementManager;
        GameCombatManager _CombatManager;
        GameEventManager _EventManager;

        public void Handled(EndCombatCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;   //忽略错误的结算信息
            var now = OwHelper.WorldNow;
            if (_CombatManager.GetTemplateById(command.CombatTId) is TemplateStringFullView ttCombat)
            {
                SimpleGameContext context = new SimpleGameContext(Guid.Empty, command.GameChar, now, null);
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

                    //杀主线副本所有怪物物数量变化事件 3fa85f64-5717-4562-b3fc-2c963f66afa6
                    _EventManager.SendEvent(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"), types_all, context);

                    //187b938c-c48a-4539-aecf-844c5665691f	杀死主线关卡精英怪数量变化事件
                    _EventManager.SendEvent(Guid.Parse("187b938c-c48a-4539-aecf-844c5665691f"), types_jingying, context);
                    //e30caeba-d6e7-4553-9ade-519a8f78a965	杀死主线关卡boss数量变化事件
                    _EventManager.SendEvent(Guid.Parse("e30caeba-d6e7-4553-9ade-519a8f78a965"), types_boss, context);
                    //8d1ea12f-26be-4fe4-acbe-ad1c7d053131	关卡中的打蛋数量成就
                    _EventManager.SendEvent(Guid.Parse("c58fe82a-6eb4-48dd-96ef-c9f30c38ccb2"), types_egg, context);
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
                }
                //处理角色经验/等级
                Guid tidJingyan = Guid.Parse("1f31807a-f633-4d3a-8e8e-382ad105d061");
                if (command.Others.Concat(command.Others).Any(c => c.TId == tidJingyan))   //若奖励了经验
                {
                    var count = command.Others.Concat(command.Others).Sum(c => c.Count);
                    _EventManager.SendEvent(Guid.Parse("6afdddd0-b98d-45fc-8f8d-41fb1f929cf8"), count, context);
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
        public ZhuxianFubenTongguanHandler(GameAchievementManager achievementManager, GameCombatManager combatManager, GameTemplateManager templateManager, GameEventManager eventManager)
        {
            _AchievementManager = achievementManager;
            _CombatManager = combatManager;
            _TemplateManager = templateManager;
            _EventManager = eventManager;
        }

        GameAchievementManager _AchievementManager;
        GameCombatManager _CombatManager;
        GameTemplateManager _TemplateManager;
        GameEventManager _EventManager;

        public void Handled(EndCombatCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            SimpleGameContext context = new SimpleGameContext(Guid.Empty, command.GameChar, now, null);
            var tt = _CombatManager.GetTemplateById(command.CombatTId);
            if (tt is null) goto lbErr;
            //0d8d8ff7-2e8a-428b-9efa-db7ee7463910	主线关卡成功通关数量变化事件（只有通过的关卡数量）
            if (command.IsSuccess && tt.Gid is int gid && gid / 1000 == 210101)   //若不是主线任务副本
            {
                var count = gid % 1000; //通关经验值
                _EventManager.SendEventWithNewValue(Guid.Parse("0d8d8ff7-2e8a-428b-9efa-db7ee7463910"), count, context);
            }
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
        public CombatStartHandler(GameAchievementManager achievementManager, GameCombatManager combatManager, GameEventManager eventManager)
        {
            _AchievementManager = achievementManager;
            _CombatManager = combatManager;
            _EventManager = eventManager;
        }

        GameAchievementManager _AchievementManager;
        GameCombatManager _CombatManager;
        GameEventManager _EventManager;

        public void Handled(StartCombatCommand command, Exception exception = null)
        {
            if (command.HasError || exception is not null) return;
            var now = OwHelper.WorldNow;
            SimpleGameContext context = new SimpleGameContext(Guid.Empty, command.GameChar, now, null);
            var tt = _CombatManager.GetTemplateById(command.CombatTId);
            /// c23f5f0a-f4b9-4a27-bad3-8556a18f83ff 主线关卡挑战次数变化事件
            if (tt is not null && tt.Gid / 1000 == 210101)    //主线关卡
            {
                _EventManager.SendEvent(Guid.Parse("c23f5f0a-f4b9-4a27-bad3-8556a18f83ff"), 1, context);
            }
            /// acbe9300-4476-47c1-a2f6-75b30e9b7b62	特殊关卡挑战次数变化事件
            if (tt is not null && tt.Gid / 100_000 == 2102)    //特殊关卡
            {
                _EventManager.SendEvent(Guid.Parse("acbe9300-4476-47c1-a2f6-75b30e9b7b62"), 1, context);
            }
        }
    }


}
