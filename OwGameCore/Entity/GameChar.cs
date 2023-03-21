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
    public class GameChar : OwGameEntityBase
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
        public GameSlot<GameEquipment> Wuqi { get; set; }

        /// <summary>
        /// 手套装备槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> ShouTao { get; set; }

        /// <summary>
        /// 衣服装备槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> YiFu { get; set; }

        /// <summary>
        /// 鞋子装备槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> XieZi { get; set; }

        /// <summary>
        /// 腰带装备槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> YaoDai { get; set; }

        /// <summary>
        /// 坐骑装备槽。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> ZuoQiSlot { get; set; }

        /// <summary>
        /// 装备背包。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> ZhuangBeiBag { get; set; }

        /// <summary>
        /// 道具背包。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> DaoJuBag { get; set; }

        /// <summary>
        /// 时装背包。
        /// </summary>
        [JsonIgnore]
        public GameSlot<GameEquipment> ShiZhuangBag { get; set; }
        #endregion 各种槽

    }

    public static class GameCharExtensions
    {
        public static object GetKey(this GameChar gc) => ((VirtualThing)gc.Thing).IdString;
    }
}
