﻿using GY02.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game.Entity
{
    /// <summary>
    /// 成就的实体类。
    /// </summary>
    [Guid("48CD339A-3B21-4DA7-9B0A-2B914B3DD42C")]
    public class GameAchievement : GameEntity
    {
        public GameAchievement()
        {
        }

        public GameAchievement(object thing) : base(thing)
        {
        }

        #region 可复制属性

        /// <summary>
        /// 各个等级的具体数据。按顺序从0开始是等级1的的情况。
        /// </summary>
        public List<GameAchievementItem> Items { get; set; } = new List<GameAchievementItem>();

        #endregion 可复制属性

        /// <summary>
        /// 按完成进度刷新等级属性。
        /// </summary>
        public void RefreshLevel(TemplateStringFullView template)
        {
            int lv = 0;
            foreach (var summary in template.Achievement.Exp2LvSequence)
            {
                if (summary > Count)    //若找到第一个未达到的项
                    break;
                lv++;
            }
            Level = lv;
        }
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
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 是否已经领取了该等级的奖励，true已经领取，false尚未领取，在未达成时此属性值也是false。
        /// </summary>
        public bool IsPicked { get; set; }

        /// <summary>
        /// 等级。从1开始，1表示达成第一级的状态，2表示达成第二级的状态，以此类推。
        /// </summary>
        public int Level { get; set; }
    }
}