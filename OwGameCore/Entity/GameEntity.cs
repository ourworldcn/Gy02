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
    public class GameEntity : GameEntityBase
    {
        public GameEntity()
        {

        }

        public GameEntity(object thing) : base(thing)
        {
        }

        /// <summary>
        /// 等级。
        /// </summary>
#if NETCOREAPP
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#endif
        [JsonPropertyName("lv")]
        public virtual decimal Level { get; set; }

        decimal _Count;

        /// <summary>
        /// 数量。
        /// </summary>
#if NETCOREAPP
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyOrder(10)]
#endif
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
                {
                    _Count = value;
                    CountOfLastModifyUtc = OwHelper.WorldNow;
                }
                else
                {
                    var dt = fcp.LastDateTime;  //保持最后计算时间点不变，如果要更新最后时间点，则应进行读取
                    fcp.SetLastValue(value, ref dt);
                    CountOfLastModifyUtc = OwHelper.WorldNow;
                }
            }
        }

        /*public decimal Count
        {
            get
            {
                //if (Guid.Parse("1f31807a-f633-4d3a-8e8e-382ad105d061") == TemplateId)
                //    ;
                if (Fcps.GetValueOrDefault(nameof(Count)) is FastChangingProperty fcp) return fcp.GetCurrentValueWithUtc(); //若是快速变化属性
                if (Thing is IDbQuickFind dqf) return dqf.ExtraDecimal ?? default;    //若有值
                return default;
            }
            set
            {
                if (Fcps.GetValueOrDefault(nameof(Count)) is FastChangingProperty fcp)
                {
                    var dt = fcp.LastDateTime;  //保持最后计算时间点不变，如果要更新最后时间点，则应进行读取
                    fcp.SetLastValue(value, ref dt);
                    CountOfLastModifyUtc = OwHelper.WorldNow;
                }
                else
                {
                    (Thing as IDbQuickFind).ExtraDecimal = value;
                    CountOfLastModifyUtc = OwHelper.WorldNow;
                }
            }
        }*/


        /// <summary>
        /// 创建此对象的世界时间。
        /// </summary>
#if NETCOREAPP
        [JsonIgnore]
#endif
        public DateTime? CreateDateTime
        {
            get
            {
                if (Thing is IDbQuickFind dqf) return dqf.ExtraDateTime ??= OwHelper.WorldNow;
                return default;
            }
            set
            {
                if (Thing is IDbQuickFind dqf) dqf.ExtraDateTime = value;
                Debug.WriteLine($"错误的Thing类型，Thing = {Thing}");
            }
        }

        /// <summary>
        /// Count 属性最后的修改时间。修改Count属性时自动修改此属性。
        /// </summary>
#if NETCOREAPP
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyOrder(11)]
#endif
        public DateTime? CountOfLastModifyUtc { get; set; }

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
        [JsonPropertyOrder(12)]
        public Dictionary<string, FastChangingProperty> Fcps { get; set; } = new Dictionary<string, FastChangingProperty>();

        /// <summary>
        /// 客户端存储的数据，服务器不使用，仅原样记录和传递。
        /// </summary>
        public Dictionary<string, string> ClientDictionary { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 按给定序列和当前对象的 <see cref="Count"/> 属性设置 <see cref="Level"/> 属性。
        /// </summary>
        /// <param name="seq">经验序列，必须严格递增。否则行为未知。</param>
        public void RefreshLevel(IList<decimal> seq)
        {
            var count = Count;
            var i = seq.Count - 1;
            for (; i >= 0; i--)
                if (count >= seq[i])    //若找到
                    break;
            Level = i + 1;
        }


        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string name;
            if ((Thing as VirtualThing)?.GetTemplate() is TemplateStringFullView tt)    //若找到模板
            {
                name = $"{tt.DisplayName} ,Count = {Count}";
            }
            else
            {
                name = $"TId = {TemplateId.ToString()[..3]}...{TemplateId.ToString()[^2..]} ,Count = {Count}";
            }
            return $"{GetType().Name}({name})";
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
        public static VirtualThing GetThing(this GameEntityBase entity)
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
