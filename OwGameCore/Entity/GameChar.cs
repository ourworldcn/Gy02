using Gy02.Publisher;
using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
        public GameSlot<GameItem> PiFuBag => (Thing as VirtualThing)?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.PiFuBagTId)?.GetJsonObject<GameSlot<GameItem>>();

        /// <summary>
        /// 货币槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameItem> HuoBiSlot => (Thing as VirtualThing)?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.HuoBiSlotTId)?.GetJsonObject<GameSlot<GameItem>>();
        #endregion 各种槽

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

        public static IEnumerable<VirtualThing> GetAllChildren(this VirtualThing root)
        {
            foreach (var item in root.Children)
            {
                yield return item;
                foreach (var item2 in item.GetAllChildren())
                    yield return item2;
            }
        }
    }
}
