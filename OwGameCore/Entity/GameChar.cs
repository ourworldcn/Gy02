using GY02.Publisher;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace OW.Game.Entity
{
    /// <summary>
    /// 游戏角色类。
    /// </summary>
    [Guid("941917CC-E91C-46D7-9F53-A98C3EB4F92E")]
    public class GameChar : GameEntity
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameChar()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="thing"><inheritdoc/></param>
        public GameChar(object thing) : base(thing)
        {
        }

        #endregion 构造函数

        #region 敏感信息

        [NotMapped]
        [JsonIgnore]
        public Guid CurrentToken { get; set; }

        #endregion 敏感信息

        #region 普通属性

        /// <summary>
        /// 角色显示用的名字。就是昵称，不可重复。
        /// </summary>
        [MaxLength(64)]
        [JsonIgnore]
        public Guid UserId { get => ((VirtualThing)Thing).ParentId ?? Guid.Empty; set => ((VirtualThing)Thing).ParentId = value; }

        /// <summary>
        /// 创建该对象的通用协调时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 是否由用户登录。
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// 攻击数值序列。
        /// </summary>
        [JsonPropertyName("atk")]
        public decimal Atk { get; set; }

        /// <summary>
        /// 防御数值序列。
        /// </summary>
        [JsonPropertyName("def")]
        public decimal Def { get; set; }

        /// <summary>
        /// 力量属性数值序列。
        /// </summary>
        [JsonPropertyName("pow")]
        public decimal Pow { get; set; }

        /// <summary>
        /// 暴击率。
        /// </summary>
        [JsonPropertyName("crit_pct")]
        public decimal CritPct { get; set; }

        /// <summary>
        /// 暴击倍数。1表示暴击和普通伤害一致。
        /// </summary>
        [JsonPropertyName("crit")]
        public decimal Crit { get; set; }

        /// <summary>
        /// 角色当前所处战斗的关卡模板Id。
        /// </summary>
        public Guid? CombatTId { get; set; }

        /// <summary>
        /// 客户端用于记录战斗内信息的字符串。
        /// </summary>
        public string ClientCombatInfo { get; set; }

        /// <summary>
        /// 该角色登录的次数。
        /// </summary>
        public int LogineCount { get; set; }

        #endregion 普通属性

        #region 简单属性

        /// <summary>
        /// 昵称。
        /// </summary>
        [JsonIgnore]
        public string DisplayName
        {
            get => ((VirtualThing)Thing).ExtraString;
            set => ((VirtualThing)Thing).ExtraString = value;
        }

        #endregion 简单属性

        #region 各种槽

        /// <summary>
        /// 已穿戴皮肤槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> YichuanPifuSlot => (Thing as VirtualThing)?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.YichuanPifuSlotTId)?.GetJsonObject<GameSlot<GameEquipment>>();

        /// <summary>
        /// 武器装备槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> Wuqi => (Thing as VirtualThing)?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.WuQiSlotTId)?.GetJsonObject<GameSlot<GameEquipment>>();

        /// <summary>
        /// 手套装备槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> ShouTao => ((VirtualThing)Thing).Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.ShouTaoSlotTId)?.GetJsonObject<GameSlot<GameEquipment>>();

        /// <summary>
        /// 衣服装备槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> YiFu => (Thing as VirtualThing)?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.YiFuSlotTId)?.GetJsonObject<GameSlot<GameEquipment>>();

        /// <summary>
        /// 鞋子装备槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> XieZi => (Thing as VirtualThing)?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.XieZiSlotTId)?.GetJsonObject<GameSlot<GameEquipment>>();

        /// <summary>
        /// 腰带装备槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> YaoDai => (Thing as VirtualThing)?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.YaoDaiSlotTId)?.GetJsonObject<GameSlot<GameEquipment>>();

        /// <summary>
        /// 坐骑装备槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> ZuoQiSlot => (Thing as VirtualThing)?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.ZuoJiSlotTId)?.GetJsonObject<GameSlot<GameEquipment>>();

        /// <summary>
        /// 装备背包。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> ZhuangBeiBag => (Thing as VirtualThing)?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.ZhuangBeiBagTId)?.GetJsonObject<GameSlot<GameEquipment>>();

        /// <summary>
        /// 道具背包。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameItem> DaoJuBag => (Thing as VirtualThing)?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.DaoJuBagTId)?.GetJsonObject<GameSlot<GameItem>>();

        /// <summary>
        /// 皮肤背包。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> PiFuBag => (Thing as VirtualThing)?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.PiFuBagTId)?.GetJsonObject<GameSlot<GameEquipment>>();

        /// <summary>
        /// 货币槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameItem> HuoBiSlot => (Thing as VirtualThing)?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.HuoBiSlotTId)?.GetJsonObject<GameSlot<GameItem>>();
        #endregion 各种槽

        #region 孵化相关

        /// <summary>
        /// 记录孵化的预览信息。
        /// </summary>
        public List<FuhuaSummary> FuhuaPreview { get; set; } = new List<FuhuaSummary>();

        /// <summary>
        /// 记录孵化系统正产出的历史数据。
        /// </summary>
        /// <remarks>是否可以生成皮肤以此处记录为准，没有则可以生成。</remarks>
        public List<FuhuaSummary> FuhuaHistory { get; set; } = new List<FuhuaSummary>();

        #endregion 孵化相关

        #region 战斗相关

        public List<CombatHistoryItem> CombatHistory { get; set; } = new List<CombatHistoryItem>();

        #endregion 战斗相关

        #region 商城相关

        /// <summary>
        /// 购买记录。
        /// </summary>
        public List<GameShoppingHistoryItem> ShoppingHistory { get; set; } = new List<GameShoppingHistoryItem>();

        #endregion 商城相关

        #region 投骰子的记录

        /// <summary>
        /// 投骰子的记录。
        /// </summary>
        public List<GameDiceHistoryItem> DiceHistory { get; set; } = new List<GameDiceHistoryItem>();
        #endregion 投骰子的记录
    }

    /// <summary>
    /// 投骰子的记录。
    /// </summary>
    public class GameDiceHistoryItem
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameDiceHistoryItem()
        {
            
        }

        /// <summary>
        /// 卡池或卡池组的TId。
        /// </summary>
        public Guid DiceTId { get; set; }

        /// <summary>
        /// 连续未命中高价值物品的次数。
        /// </summary>
        public int GuaranteesCount { get; set; }
    }

    /// <summary>
    /// 购买记录。
    /// </summary>
    public class GameShoppingHistory : Collection<GameShoppingHistoryItem>
    {
        public GameShoppingHistory()
        {
            
        }
    }

    /// <summary>
    /// 商品项的状态描述对象。
    /// </summary>
    public class ShoppingItemState
    {
        /// <summary>
        /// 默认构造函数。
        /// </summary>
        public ShoppingItemState()
        {

        }

        /// <summary>
        /// 商品项的模板Id。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 当前周期下的已经购买数量。
        /// </summary>
        public decimal BuyedCount { get; set; }

        /// <summary>
        /// 记录该项属于的购买周期的起始时间点。
        /// </summary>
        public DateTime StartUtc { get; set; }

        /// <summary>
        /// 本周期的结束时间。空表示无结束时间。
        /// </summary>
        public DateTime? EndUtc { get; set; }
    }

    /// <summary>
    /// 购买记录的详细项。
    /// </summary>
    public class GameShoppingHistoryItem
    {
        /// <summary>
        /// 默认构造函数。
        /// </summary>
        public GameShoppingHistoryItem()
        {

        }

        /// <summary>
        /// 购买的商品TId。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 购买的商品的次数。如两次可能购买总计2000金币，但这里是2。具体获得物品的数量取决于商品项的配置。
        /// </summary>
        public decimal Count { get; set; }

        /// <summary>
        /// 购买的日期。
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// 该项是否有效。在同组数据返回时，有些项是无效项。
        /// </summary>
        /// <value>true有效数据，false无效数据，无效数据包含已达最大购买数量，已过期，未到期三种情况。</value>
        public bool Valid { get; set; }
    }

    /// <summary>
    /// 战斗历史记录的详细项。
    /// </summary>
    public class CombatHistoryItem
    {
        /// <summary>
        /// 关卡的模板Id。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 该关卡的最短时间，如果null,表示没有记录过。
        /// </summary>
        public TimeSpan? MinTimeSpanOfPass { get; set; }
    }

    /// <summary>
    /// 孵化预览信息。
    /// </summary>
    public class FuhuaSummary
    {
        /// <summary>
        /// 双亲的TId集合，目前有两个元素，且按升序排序。
        /// </summary>
        public List<string> ParentTIds { get; set; } = new List<string>();

        /// <summary>
        /// 可能产出的物品预览。
        /// </summary>
        public List<GameDiceItemSummary> Items { get; set; } = new List<GameDiceItemSummary>();
    }

    /// <summary>
    /// 生成项的摘要信息。
    /// </summary>
    public class GameDiceItemSummary
    {
        /// <summary>
        /// 生成项的摘要。
        /// </summary>
        public GameEntitySummary Entity { get; set; }

        /// <summary>
        /// 生成项的权重。
        /// </summary>
        public decimal Weight { get; set; }
    }

    public static class GameCharExtensions
    {
        public static object GetKey(this GameChar gc) => ((VirtualThing)gc.Thing).IdString;

        /// <summary>
        /// 获取用户。
        /// </summary>
        /// <param name="gc"></param>
        /// <returns></returns>
        public static GameUser GetUser(this GameChar gc) => ((VirtualThing)gc.Thing).Parent.GetJsonObject<GameUser>();

        /// <summary>
        /// 获取属于指定角色的所有子虚拟物。
        /// </summary>
        /// <param name="gc"></param>
        /// <returns>所有子虚拟物的枚举子。如果出错则返回null,此时用<see cref="OwHelper.GetLastError"/>确定具体信息。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<VirtualThing> GetAllChildren(this GameChar gc) => gc.GetThing()?.GetAllChildren();
    }
}
