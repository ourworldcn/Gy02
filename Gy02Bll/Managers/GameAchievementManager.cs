using GY02.Base;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{
    public class GameAchievementManagerOptions : IOptions<GameAchievementManagerOptions>
    {
        public GameAchievementManagerOptions Value => this;
    }

    /// <summary>
    /// 成就/任务管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameAchievementManager : GameManagerBase<GameAchievementManagerOptions, GameAchievementManager>
    {
        public GameAchievementManager(IOptions<GameAchievementManagerOptions> options, ILogger<GameAchievementManager> logger, GameTemplateManager templateManager, GameEntityManager entityManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
            _EntityManager = entityManager;
        }

        GameTemplateManager _TemplateManager;
        GameEntityManager _EntityManager;

        #region 基础操作

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <returns>返回null是出错，此时调用<see cref="OwHelper.GetLastError()"/>获取详细信息。</returns>
        public GameAchievementTO GetAchievementByTemplate(TemplateStringFullView template)
        {
            var result = template.Achievement;
            if (result is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"指定的模板没有成就的定义，TId={template.TemplateId}");
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns>返回null是出错，此时调用<see cref="OwHelper.GetLastError()"/>获取详细信息。</returns>
        public TemplateStringFullView GetTemplateById(Guid id)
        {
            var result = _TemplateManager.GetFullViewFromId(id);
            if (result is null) return null;
            if (GetAchievementByTemplate(result) is null) return null;
            return result;
        }

        /// <summary>
        /// 获取指定的成就对象，如果没有则创建。
        /// </summary>
        /// <param name="gameChar">角色。</param>
        /// <param name="template">模板。</param>
        /// <param name="changes">记录变化数据。</param>
        /// <returns>null表示出错。此时调用<see cref="OwHelper.GetLastError()"/>获取详细信息。</returns>
        public GameAchievement GetOrCreate(GameChar gameChar, TemplateStringFullView template, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var to = GetAchievementByTemplate(template);
            if (to is null) return null;
            var slotThing = gameChar.ChengJiuSlot.GetThing();    //成就槽的虚拟对象。
            if (slotThing is null) return null;
            var thing = slotThing.Children.FirstOrDefault(c => c.ExtraGuid == template.TemplateId); //成就对象
            if (thing is null)   //若需要初始化
            {
                var list = _EntityManager.Create(new GameEntitySummary { TId = template.TemplateId, Count = 0 });
                thing = list.First().GetThing();
                _EntityManager.Move(list, gameChar.ChengJiuSlot, changes);
            }
            return thing.GetJsonObject<GameAchievement>();
        }

        /// <summary>
        /// 初始化每个等级的状态。
        /// </summary>
        /// <param name="achievement"></param>
        /// <returns>true成功，false出错，此时调用<see cref="OwHelper.GetLastError()"/>获取详细信息。</returns>
        public bool InitializeState(GameAchievement achievement)
        {
            var tt = GetTemplateById(achievement.TemplateId);
            if (tt is null) return false;
            if (achievement.Items.Count == 0)   //若需要初始化
            {
                for (int i = 0; i < tt.Achievement.Exp2LvSequence.Length; i++)
                {
                    var state = new GameAchievementItem
                    {
                        IsCompleted = false,
                        IsPicked = false,
                        Level = i + 1,
                    };
                    if (tt.Achievement.Outs.Count > i && tt.Achievement.Outs[i] is GameEntitySummary[] ary)
                        state.Rewards.AddRange(ary); //TODO 转换
                    achievement.Items.Add(state);
                }
            }
            return true;
        }

        #endregion 基础操作

        #region 功能性操作

        /// <summary>
        /// 刷新状态。
        /// </summary>
        /// <param name="achievement"></param>
        /// <returns>true成功，false出错，此时调用<see cref="OwHelper.GetLastError()"/>获取详细信息。</returns>
        public bool RefreshState(GameAchievement achievement)
        {
            var tt = GetTemplateById(achievement.TemplateId);
            if (tt is null) return false;
            achievement.RefreshLevel(tt);
            if (achievement.Items.Count == 0)    //若可能需要初始化
                if (!InitializeState(achievement)) return false;

            achievement.Items.ForEach(state => state.IsCompleted = achievement.Level >= state.Level);
            return true;
        }

        /// <summary>
        /// 给指定成就的指标值增加增量。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="template">成就的模板。</param>
        /// <param name="exp">成就经验值的增量</param>
        /// <param name="changes">记录变化数据。如果成就对象尚未初始化，可能会增加一个成就对象。</param>
        /// <returns>true成功，false出错，此时调用<see cref="OwHelper.GetLastError()"/>获取详细信息。</returns>
        public bool AddExperience(GameChar gameChar, TemplateStringFullView template, decimal exp, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var achi = GetOrCreate(gameChar, template, changes);  //成就对象
            if (achi is null) return false;
            if (exp == 0) return true;

            var oldLv = achi.Level;
            if (!achi.ModifyPropertyAndMarkChanges(nameof(achi.Count), achi.Count + exp, changes)) return false;

            achi.RefreshLevel(template);    //刷新等级

            changes?.MarkChanges(achi, nameof(achi.Level), oldLv, achi.Level);
            return true;
        }

        /// <summary>
        /// 领取奖励。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="template"></param>
        /// <param name="levels">要领取奖励的等级的索引集合。第一个能够完成的等级是1。</param>
        /// <param name="changes">记录变化数据。如果成就对象尚未初始化，可能会增加一个成就对象。</param>
        /// <returns>true成功，false出错，此时调用<see cref="OwHelper.GetLastError()"/>获取详细信息。</returns>
        public bool GetRewards(GameChar gameChar, TemplateStringFullView template, IEnumerable<int> levels, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var achi = GetOrCreate(gameChar, template, changes);  //成就对象
            if (levels.Any(c => c < 1 || c > achi.Count))   //若参数出错
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"{nameof(levels)}参数中有一项出错，小于0或大于了最大等级");
                return false;
            }
            if (achi is null) return false; //若找不到指定成就对象或创建失败
            if (!RefreshState(achi)) return false;   //若刷新状态失败
            var errorItem = achi.Items.FirstOrDefault(c => !levels.Contains(c.Level) || !c.IsCompleted || c.IsPicked);
            if (errorItem is not null)  //若有错误
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"指定的等级中至少一个不能领取，TId={template.TemplateId} ,Level={errorItem.Level}");
                return false;
            }
            var baseColl = achi.Items.Where(c => levels.Contains(c.Level)).ToArray();
            var summaries = baseColl.SelectMany(c => c.Rewards);
            var entities = _EntityManager.Create(summaries);
            if (entities is null) return false;

            _EntityManager.Move(entities.Select(c => c.Item2), gameChar, changes);
            baseColl.ForEach(c =>
            {
                c.IsPicked = true;
                changes?.MarkChanges(c, nameof(c.IsPicked), false, true);   //标记变化项
            });
            return true;
        }
        #endregion 功能性操作
    }

}
