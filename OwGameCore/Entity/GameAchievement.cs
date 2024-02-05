using GY02.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OW.Game.Entity
{
    /// <summary>
    /// 成就的实体类。
    /// </summary>
    [Guid("48CD339A-3B21-4DA7-9B0A-2B914B3DD42C")]
    public class GameAchievement : GameEntity
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameAchievement()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="thing"></param>
        public GameAchievement(object thing) : base(thing)
        {
        }

        #region 可复制属性

        /// <summary>
        /// 各个等级的具体数据。按顺序从0开始是等级1的的情况。
        /// </summary>
        public List<GameAchievementItem> Items { get; set; } = new List<GameAchievementItem>();

        /// <summary>
        /// 当前该成就/任务是否有效。true有效，false无效此时成就任务的计数不会推进，但已有的奖励仍然可以领取（若已完成且未领取）。UI可以在无效时不让领取。
        /// </summary>
#if NET5_0_OR_GREATER
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#endif
        public bool IsValid { get; set; }

        /// <summary>
        /// 最后一次修改的时间。
        /// </summary>
        public DateTime LastModifyDateTime { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 最后一次标记的自传周期号。用于解决周期结束开始之间需要发生两个事件的问题。
        /// </summary>
#if NET5_0_OR_GREATER
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#endif
        public decimal? LastMarkPeriodIndex { get; set; }

        #endregion 可复制属性
    }

    /// <summary>
    /// 成就每个级别的状态。
    /// </summary>
    public class GameAchievementItem
    {
        /// <summary>
        /// 奖励。注意该奖励是经过翻译的，即不会包含序列和卡池项。在生成该项时会确定随机性（虽然一般不会有随机奖励）。
        /// 若需要找到原定义去找对应的模板数据。
        /// </summary>
        public List<GameEntitySummary> Rewards { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// 是否已经达成该等级。true已经达成，false未达成。
        /// </summary>
#if NET5_0_OR_GREATER
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#endif
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 是否已经领取了该等级的奖励，true已经领取，false尚未领取，在未达成时此属性值也是false。
        /// </summary>
#if NET5_0_OR_GREATER
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#endif
        public bool IsPicked { get; set; }

        /// <summary>
        /// 等级。1表示第一级的状态，2表示第二级的状态，以此类推。
        /// </summary>
#if NET5_0_OR_GREATER
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#endif
        public int Level { get; set; }
    }
}
