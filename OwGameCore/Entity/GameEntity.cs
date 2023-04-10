using Gy02Bll.Templates;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OW.Game.Entity
{
    /// <summary>
    /// 游戏内装备/道具的摘要信息。
    /// </summary>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    [DisplayName("虚拟物摘要")]
    public class GameEntitySummary
    {
        public GameEntitySummary()
        {

        }

        /// <summary>
        /// 唯一Id。保留未用。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 父容器模板Id，保留未用。
        /// </summary>
        public Guid? ParentTId { get; set; }

        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 数量。
        /// </summary>
        public decimal Count { get; set; }

        private string GetDebuggerDisplay()
        {
            return $"Summary({TId},{Count})";
        }
    }

    public static class GameEntitySummaryExtensions
    {
        public static void AddGameEntitySummary(this ICollection<GamePropertyChangeItem<object>> changes, GameEntity entity, GameEntitySummary summary)
        {
            changes.Add(new GamePropertyChangeItem<object>
            {
                Object = entity,
                PropertyName = nameof(entity.LvUpAccruedCost),

                HasOldValue = false,
                OldValue = default,

                HasNewValue = true,
                NewValue = summary,
            });
        }
    }

    /// <summary>
    /// 游戏内非容器的虚拟物的实体基类。
    /// </summary>
    public class GameEntity : OwGameEntityBase
    {
        public GameEntity()
        {

        }

        public GameEntity(object thing) : base(thing)
        {
        }

        [JsonPropertyName("lv")]
        public decimal Level { get; set; }

        [JsonPropertyName("count")]
        public decimal Count { get; set; }

        /// <summary>
        /// 升级的累计消耗。
        /// </summary>
        public List<GameEntitySummary> LvUpAccruedCost { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// 升品的累计用品。
        /// </summary>
        public List<GameEntitySummary> CompositingAccruedCost { get; set; } = new List<GameEntitySummary>();

        public override string ToString()
        {
            var name = ((Thing as VirtualThing)?.GetTemplate())?.DisplayName;
            return $"{base.ToString()}({name})";
        }
    }

    public class GameContainer : GameEntity
    {
        public GameContainer()
        {
        }

        public GameContainer(object thing) : base(thing)
        {
        }

        /// <summary>
        /// 容器的容量。
        /// </summary>
        [JsonPropertyName("cap")]
        public decimal Capacity { get; set; } = -1;
    }
}
