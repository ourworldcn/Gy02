using GY02.Base;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.DDD;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GY02.Managers
{
    public class GameAchievementManagerOptions : IOptions<GameAchievementManagerOptions>
    {
        public GameAchievementManagerOptions Value => this;
    }

    public class AchievementChangedEventArgs : EventArgs
    {
        public AchievementChangedEventArgs()
        {

        }

        public AchievementChangedEventArgs(GameAchievement achievement)
        {
            Achievement = achievement;
        }

        public GameAchievement Achievement { get; internal set; }
    }

    /// <summary>
    /// 成就/任务管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class GameAchievementManager : GameManagerBase<GameAchievementManagerOptions, GameAchievementManager>
    {
        public GameAchievementManager(IOptions<GameAchievementManagerOptions> options, ILogger<GameAchievementManager> logger, GameTemplateManager templateManager, GameEntityManager entityManager, GameBlueprintManager blueprintManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
            _EntityManager = entityManager;
            Task.Run(() =>
            {
                var tts = Templates;
                return tts;
            });
            _BlueprintManager = blueprintManager;
        }

        GameTemplateManager _TemplateManager;
        GameEntityManager _EntityManager;
        GameBlueprintManager _BlueprintManager;
        ConcurrentDictionary<Guid, TemplateStringFullView> _Templates;

        /// <summary>
        /// 所有任务/成就模板。
        /// </summary>
        /// <remarks>支持并发访问。</remarks>
        public ConcurrentDictionary<Guid, TemplateStringFullView> Templates
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _Templates, () =>
                {
                    var coll = from tmp in _TemplateManager.Id2FullView
                               where tmp.Value.Achievement is not null
                               select tmp;
                    return new ConcurrentDictionary<Guid, TemplateStringFullView>(coll);
                });
                return _Templates;
            }
        }

        #region 事件及相关

        public event EventHandler<AchievementChangedEventArgs> AchievementChanged;

        /// <summary>
        /// 引发成就/任务项，变化事件。
        /// </summary>
        /// <param name="args"></param>
        public void InvokeAchievementChanged(AchievementChangedEventArgs args) => AchievementChanged?.Invoke(this, args);

        /// <summary>
        /// 对指定的成就任务项增加计数，若等级发生变化则引发事件（通过<see cref="InvokeAchievementChanged(AchievementChangedEventArgs)"/>）
        /// </summary>
        /// <param name="achievement"></param>
        /// <param name="inc">任务/成就项的计数值的增量数值，小于或等于0则立即返回false。</param>
        /// <param name="gameChar"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public bool RaiseEventIfLevelChanged(GameAchievement achievement, decimal inc, GameChar gameChar, DateTime now)
        {
            if (inc <= 0) return false;
            var olv = achievement.Level;
            achievement.Count += inc;
            if (!RefreshState(achievement, gameChar, now)) return false;
            if (achievement.Level > olv)
                InvokeAchievementChanged(new AchievementChangedEventArgs { Achievement = achievement });
            return true;
        }

        /// <summary>
        /// 对指定的成就任务项增加计数，若等级发生变化则引发事件（通过<see cref="InvokeAchievementChanged(AchievementChangedEventArgs)"/>）
        /// </summary>
        /// <param name="achievementTId"></param>
        /// <param name="inc"></param>
        /// <param name="gameChar"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public bool RaiseEventIfLevelChanged(Guid achievementTId, decimal inc, GameChar gameChar, DateTime now)
        {
            var tt = GetTemplateById(achievementTId);
            if (tt is null) return false;
            var achi = GetOrCreate(gameChar, tt);
            if (achi is null) return false;
            return RaiseEventIfLevelChanged(achi, inc, gameChar, now);
        }

        #endregion 事件及相关

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
        /// <param name="gameChar"></param>
        /// <param name="now"></param>
        /// <returns>true成功，false出错，此时调用<see cref="OwHelper.GetLastError()"/>获取详细信息。</returns>
        public bool RefreshState(GameAchievement achievement, GameChar gameChar, DateTime now)
        {
            var tt = GetTemplateById(achievement.TemplateId);
            if (tt is null) return false;
            achievement.RefreshLevel(tt);
            if (achievement.Items.Count == 0)    //若可能需要初始化
                if (!InitializeState(achievement)) return false;

            //刷新有效性
            achievement.IsValid = IsValid(achievement, gameChar, now);
            //刷新每个等级的达成标志
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
            var now = OwHelper.WorldNow;
            var achi = GetOrCreate(gameChar, template, changes);  //成就对象
            if (levels.Any(c => c < 1 || c > achi.Count))   //若参数出错
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"{nameof(levels)}参数中有一项出错，小于0或大于了最大等级");
                return false;
            }
            if (achi is null) return false; //若找不到指定成就对象或创建失败
            if (!RefreshState(achi, gameChar, now)) return false;   //若刷新状态失败
            var errorItem = achi.Items.FirstOrDefault(c => levels.Contains(c.Level) && (!c.IsCompleted || c.IsPicked));
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
            baseColl.ForEach(c => c.IsPicked = true);
            changes?.Add(new GamePropertyChangeItem<object>
            {
                Object = achi,
                PropertyName = nameof(achi.Items),

                HasNewValue = true,
                NewValue = achi.Items,
            });
            return true;
        }

        /// <summary>
        /// 检测指定任务/成就模板的有效性。
        /// </summary>
        /// <param name="achiTId"></param>
        /// <param name="gameChar"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public bool IsValid(Guid achiTId, GameChar gameChar, DateTime now)
        {
            if (GetTemplateById(achiTId) is not TemplateStringFullView tt) return false;
            if (GetOrCreate(gameChar, tt) is not GameAchievement achi) return false;
            return IsValid(achi, gameChar, now);
        }

        /// <summary>
        /// 获取指示该成就/任务对象是否在有效状态。
        /// </summary>
        /// <param name="achievement"></param>
        /// <param name="gameChar"></param>
        /// <param name="now"></param>
        /// <returns>true有效，false无效。</returns>
        public bool IsValid(GameAchievement achievement, GameChar gameChar, DateTime now)
        {
            var tt = GetTemplateById(achievement.TemplateId);
            if (tt == null) goto lbErr;
            if (!tt.Achievement.Period.IsValid(now, out _))
                return false;

            var b = tt.Achievement.Ins is null || _BlueprintManager.IsValid(tt.Achievement.Ins, _EntityManager.GetAllEntity(gameChar));
            if (!b)
                return false;

            return true;
        lbErr:
            return false;
        }

        /// <summary>
        /// 获取含指定类属且特别标记了TId的任务/成就模板。
        /// </summary>
        /// <param name="genus"></param>
        /// <param name="insTId"></param>
        /// <returns></returns>
        public TemplateStringFullView GetTemplate(string genus, Guid insTId)
        {
            var achiTt = Templates.Where(c => c.Value.Genus?.Contains(genus) ?? false).FirstOrDefault(c => //实体对应的图鉴成就
            {
                var ary = c.Value.Achievement?.TjIns;
                if (ary is null || ary.Length <= 0) return false;
                return ary[0].Conditional?[0].TId == insTId;
            }).Value;
            return achiTt;
        }
        #endregion 功能性操作

    }

}
