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
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
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
        public GameAchievementManager(IOptions<GameAchievementManagerOptions> options, ILogger<GameAchievementManager> logger, GameTemplateManager templateManager, GameEntityManager entityManager, GameBlueprintManager blueprintManager, SpecialManager specialManager) : base(options, logger)
        {
            _TemplateManager = templateManager;
            _EntityManager = entityManager;
            Task.Run(() =>
            {
                var tts = Templates;
                return tts;
            });
            _BlueprintManager = blueprintManager;
            Initialize();
            _SpecialManager = specialManager;
        }

        private void Initialize()
        {
        }

        GameTemplateManager _TemplateManager;
        GameEntityManager _EntityManager;
        GameBlueprintManager _BlueprintManager;
        ConcurrentDictionary<Guid, TemplateStringFullView> _Templates;
        SpecialManager _SpecialManager;

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
        /// 对指定的成就任务项增加计数，若计数发生变化则引发事件（通过<see cref="InvokeAchievementChanged(AchievementChangedEventArgs)"/>）
        /// </summary>
        /// <param name="achievement"></param>
        /// <param name="inc">任务/成就项的计数值的增量数值，小于或等于0则立即返回false。</param>
        /// <param name="gameChar"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public bool RaiseEventIfChanged(GameAchievement achievement, decimal inc, GameChar gameChar, DateTime now)
        {
            if (inc <= 0) return false;
            var olv = achievement.Level;
            achievement.Count += inc;
            if (!RefreshState(achievement, gameChar, now)) return false;
            if (inc > 0)
                InvokeAchievementChanged(new AchievementChangedEventArgs { Achievement = achievement });
            return true;
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
        /// 获取含指定类属且特别标记了TId的任务/成就模板。
        /// </summary>
        /// <param name="genus"></param>
        /// <param name="insTId"></param>
        /// <returns></returns>
        public TemplateStringFullView GetTemplateByGenus(string genus, Guid insTId)
        {
            var achiTt = Templates.Where(c => c.Value.Genus?.Contains(genus) ?? false).FirstOrDefault(c => //实体对应的图鉴成就
            {
                var ary = c.Value.Achievement?.TjIns;
                if (ary is null || ary.Length <= 0) return false;
                return ary[0].Conditional?[0].TId == insTId;
            }).Value;
            return achiTt;
        }

        /// <summary>
        /// 获取指定的成就对象，如果没有则创建并自动初始化。
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
            var result = thing.GetJsonObject<GameAchievement>();
            if (result.Items.Count <= 0) InitializeState(result);
            return result;
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
                    {
                        state.Rewards.AddRange(ary); //TODO 转换
                    }
                    achievement.Items.Add(state);
                }
                achievement.LastModifyDateTime = OwHelper.WorldNow;
            }
            return true;
        }

        /// <summary>
        /// 给指定成就的经验设置新值。忽略了初始化，有效性，切换周期等问题。
        /// </summary>
        /// <param name="achi">成就。</param>
        /// <param name="newExp">成就新经验值。即使经验值没有变换，也会试图刷新等级。</param>
        /// <param name="context">该处理所处上下文。</param>
        /// <returns>true成功，false出错，此时调用<see cref="OwHelper.GetLastError()"/>获取详细信息。</returns>
        public bool SetExperience(GameAchievement achi, decimal newExp, IGameContext context)
        {
            if (GetTemplateById(achi.TemplateId) is not TemplateStringFullView tt) return false;
            var oldLv = achi.Level;
            if (achi.Count != newExp)   //若经验变化了
            {
                var oldCount = achi.Count;
                achi.Count = newExp;
                context.Changes?.MarkChanges(achi, nameof(achi.Count), oldCount, achi.Count);
            }

            achi.RefreshLevel(tt.Achievement.Exp2LvSequence);    //刷新等级
            if (oldLv != achi.Level)   //若等级变化了
            {
                context.Changes?.MakeLevelChanged(achi, oldLv, achi.Level);
            }
            //刷新每个等级的达成标志
            achi.IsValid = IsValid(achi, context.GameChar, context.WorldDateTime);
            if (achi.IsValid)
                achi.Items.ForEach(state => state.IsCompleted = achi.Level >= state.Level);
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
            if (GetTemplateById(achievement.TemplateId) is not TemplateStringFullView tt) return false;
            //刷新有效性
            achievement.IsValid = IsValid(achievement, gameChar, now);
            if (!achievement.IsValid) achievement.Count = 0;

            achievement.RefreshLevel(tt.Achievement.Exp2LvSequence);
            if (achievement.Items.Count == 0)    //若可能需要初始化
                if (!InitializeState(achievement)) return false;

            //刷新每个等级的达成标志
            if (achievement.IsValid)
                achievement.Items.ForEach(state => state.IsCompleted = achievement.Level >= state.Level);
            else //若处于无效状态
            {
                achievement.Count = 0; achievement.Level = 0;
                achievement.Items.ForEach(state =>
                {
                    state.IsCompleted = false;
                    state.IsPicked = false;
                });
            }

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
            var summaries = baseColl.SelectMany(c => c.Rewards).ToList();
            #region 不满足条件时排除赛季积分货币
            var saijiBaoxianHuobiTid = Guid.Parse("e93c4aa4-ab58-4262-a93b-0118edfe8afb");  //赛季开宝箱货币
            if (summaries.Any(c => c.TId == saijiBaoxianHuobiTid))  //若有赛季积分货币
            {
                var tt = GetTemplateById(Guid.Parse("008d96c5-9545-44fb-965d-6d1c9d97f97d"));   //赛季免费门票成就
                var saiji = GetOrCreate(gameChar, tt);
                if (saiji.Count < tt.Achievement.Exp2LvSequence.Last()) //已经满级
                {
                    summaries.RemoveAll(c => c.TId == saijiBaoxianHuobiTid);
                }
            }
            #endregion 不满足条件时排除赛季积分货币
            var list = new List<(GameEntitySummary, IEnumerable<GameEntitySummary>)> { };
            var b = _SpecialManager.Transformed(summaries, list, new EntitySummaryConverterContext
            {
                Change = null,
                GameChar = gameChar,
                IgnoreGuarantees = false,
                Random = new Random(),
            });

            var entities = _EntityManager.Create(list.SelectMany(c => c.Item2));
            if (entities is null) return false;

            _EntityManager.Move(entities.Select(c => c.Item2), gameChar, changes);
            //判断有没有连锁的变化
            if (changes?.Select(c => c.Object as GameAchievement).OfType<GameAchievement>().ToArray() is GameAchievement[] achis) //找到新增或变化的任务/成就
            {
                foreach (var tmp in achis)
                {
                    var olv = tmp.Level;
                    if (!RefreshState(tmp, gameChar, now)) continue;
                    if (tmp.Level > olv)    //若等级发生了变化
                    {
                        changes?.MakeLevelChanged(tmp, olv, tmp.Level);
                    }
                }
            }
            //设置拾取标志变化
            baseColl.ForEach(c =>
            {
                c.IsPicked = true;
            });
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

            var b = tt.Achievement.Ins is null || _BlueprintManager.GetMatches(_EntityManager.GetAllEntity(gameChar), tt.Achievement.Ins, 1).All(c => c.Item1 is not null);
            if (!b)
                return false;

            return true;
        lbErr:
            return false;
        }

        #endregion 功能性操作

    }

    public static class GameAchievementManagerExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="achievementMng"></param>
        /// <param name="gameChar"></param>
        /// <param name="achiTId"></param>
        /// <returns>null表示出错。此时调用<see cref="OwHelper.GetLastError()"/>获取详细信息。</returns>
        public static GameAchievement GetOrCreate(this GameAchievementManager achievementMng, GameChar gameChar, Guid achiTId)
        {
            if (achievementMng.GetTemplateById(achiTId) is not TemplateStringFullView tt) return null;
            return achievementMng.GetOrCreate(gameChar, tt);
        }

        /// <summary>
        /// 如果新的值大于成就对象已有经验值则刷新对象状态。
        /// </summary>
        /// <param name="achievementManager"></param>
        /// <param name="achiTId"></param>
        /// <param name="newValue">仅当新值大于旧值时惨生效。</param>
        /// <param name="gameChar"></param>
        /// <param name="now"></param>
        /// <returns>true成功设置，false新值小于或等于旧值。</returns>
        public static bool RaiseEventIfSetAndChanged(this GameAchievementManager achievementManager, Guid achiTId, decimal newValue, GameChar gameChar, DateTime now)
        {
            if (achievementManager.GetOrCreate(gameChar, achiTId) is not GameAchievement achi) return false;
            if (achi.Count >= newValue) return false;
            return achievementManager.RaiseEventIfChanged(achi, newValue - achi.Count, gameChar, now);
        }

        /// <summary>
        /// 对指定的成就任务项增加计数，若计数发生变化则引发事件（通过<see cref="InvokeAchievementChanged(AchievementChangedEventArgs)"/>）
        /// </summary>
        /// <param name="achiTId"></param>
        /// <param name="inc"></param>
        /// <param name="gameChar"></param>
        /// <param name="now"></param>
        /// <returns></returns>

        public static bool RaiseEventIfIncreaseAndChanged(this GameAchievementManager achievementManager, Guid achiTId, decimal inc, GameChar gameChar, DateTime now)
        {
            if (inc <= 0) return false;
            if (achievementManager.GetOrCreate(gameChar, achiTId) is not GameAchievement achi) return false;
            return achievementManager.RaiseEventIfChanged(achi, inc, gameChar, now);
        }

        /// <summary>
        /// 标记等级变化。
        /// </summary>
        /// <param name="coll"></param>
        /// <param name="achi"></param>
        /// <param name="oldLevel"></param>
        /// <param name="newLevel"></param>
        public static void MakeLevelChanged(this ICollection<GamePropertyChangeItem<object>> coll, GameAchievement achi, decimal oldLevel, decimal newLevel)
        {
            Debug.Assert(oldLevel != newLevel);
            coll?.MarkChanges(achi, nameof(achi.Level), oldLevel, newLevel);
            coll?.Add(new GamePropertyChangeItem<object>
            {
                Object = achi,
                PropertyName = nameof(achi.Items),

                HasNewValue = true,
                NewValue = achi.Items,
            });

        }
    }
}
