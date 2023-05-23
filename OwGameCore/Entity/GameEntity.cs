using GY02.Publisher;
using GY02.Templates;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace OW.Game.Entity
{
    /// <summary>
    /// 游戏内装备/道具的摘要信息。
    /// </summary>
    [DisplayName("虚拟物摘要")]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class GameEntitySummary
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
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

        decimal _Count;
        //[JsonPropertyName("count")]
        public decimal Count
        {
            get
            {
                var fcp = Fcps.GetValueOrDefault(nameof(Count));
                return fcp is null ? _Count : fcp.GetCurrentValueWithUtc();
            }
            set
            {
                var fcp = Fcps.GetValueOrDefault(nameof(Count));
                if (fcp is null)
                    _Count = value;
                else
                {
                    var dt = DateTime.UtcNow;
                    fcp.SetLastValue(value, ref dt);
                }
            }
        }

        /// <summary>
        /// 升级的累计消耗。
        /// </summary>
        public List<GameEntitySummary> LvUpAccruedCost { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// 升品的累计用品。
        /// </summary>
        public List<GameEntitySummary> CompositingAccruedCost { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// 快速变化属性的字典集合，键是属性名，值快速变化属性的对象。
        /// </summary>
        public Dictionary<string, FastChangingProperty> Fcps { get; set; } = new Dictionary<string, FastChangingProperty>();

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

    public static class GameEntityExtensions
    {
        /// <summary>
        /// 获取实体寄宿的虚拟对象。
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>虚拟对象，如果出错则返回null,此时用<see cref="OwHelper.GetLastError"/>确定具体信息。</returns>
        public static VirtualThing GetThing(this GameEntity entity)
        {
            var result = entity.Thing as VirtualThing;
            if (result == null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"指定实体寄宿的对象类型不是{typeof(VirtualThing)}类型,Id={entity.Id}");
            }
            return result;
        }
    }
}
