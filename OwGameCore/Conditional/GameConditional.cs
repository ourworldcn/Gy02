using System.Collections.Generic;
using System;

namespace OW.Game.Conditional
{
    public interface IGamePrecondition
    {

    }

    public class GamePropertyCondition
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GamePropertyCondition()
        {

        }

        public string Operator { get; set; }

        public string PropertyName { get; set; }

        public object Value { get; set; }

        public bool Conform(object obj)
        {
            return true;
        }
    }

    /// <summary>
    /// 寻找一个物品的条件对象。
    /// </summary>
    public class GameThingPrecondition
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameThingPrecondition()
        {

        }

        /// <summary>
        /// 父容器的模板Id。
        /// 省略表示不限制。
        /// </summary>
        public Guid? PTId { get; set; }

        /// <summary>
        /// 对象的模板Id。
        /// 省略表示不限制。
        /// </summary>
        public Guid? TId { get; set; }

        /// <summary>
        /// 属的限制。空集合表示不限制，多个属，表示任一个都符合条件。
        /// </summary>
        public List<string> Genus { get; set; } = new List<string>();

        /// <summary>
        /// 属性相关的条件。
        /// </summary>
        public List<GamePropertyCondition> PropertyConditions { get; set; } = new List<GamePropertyCondition>();

        /// <summary>
        /// 消耗的数量。
        /// 可以不填表示不消耗，但仍需有该条件
        /// </summary>
        public decimal? DepleteCount { get; set; }
    }

}