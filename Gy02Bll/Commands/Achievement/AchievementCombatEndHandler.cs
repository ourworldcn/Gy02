using GY02.Managers;
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
    public class AchievementCombatEndHandler : SyncCommandHandlerBase<CombatEndCommand>
    {
        public AchievementCombatEndHandler(GameTemplateManager templateManager, GameAchievementManager achievementManager)
        {
            _TemplateManager = templateManager;
            _AchievementManager = achievementManager;
        }

        GameTemplateManager _TemplateManager;
        GameAchievementManager _AchievementManager;

        public override void Handle(CombatEndCommand command)
        {
            if (command.HasError) return;   //忽略错误的结算信息
            var now = OwHelper.WorldNow;
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
            _AchievementManager.RaiseEventIfLevelChanged(achievement, types_all, command.GameChar, now);
            //杀死精英怪物数量成就
            ttAchi = _AchievementManager.GetTemplateById(new Guid("ad102f96-b971-460b-a894-9fde078fee4d"));
            achievement = _AchievementManager.GetOrCreate(command.GameChar, ttAchi);
            _AchievementManager.RaiseEventIfLevelChanged(achievement, types_jingying, command.GameChar, now);
            //杀死Boss怪物数量成就
            ttAchi = _AchievementManager.GetTemplateById(new Guid("21889fad-e13e-4b8a-b580-f31274aa9d65"));
            achievement = _AchievementManager.GetOrCreate(command.GameChar, ttAchi);
            _AchievementManager.RaiseEventIfLevelChanged(achievement, types_boss, command.GameChar, now);
            //8d1ea12f-26be-4fe4-acbe-ad1c7d053131	关卡中的打蛋数量成就
            ttAchi = _AchievementManager.GetTemplateById(new Guid("8d1ea12f-26be-4fe4-acbe-ad1c7d053131"));
            achievement = _AchievementManager.GetOrCreate(command.GameChar, ttAchi);
            _AchievementManager.RaiseEventIfLevelChanged(achievement, types_egg, command.GameChar, now);
            #endregion 杀怪数量 
            //2913b8e2-3db3-4204-b36c-415d6bc6b3f0	闯关数量成就
        }
    }
}
