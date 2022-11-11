using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OW.Game.Entity
{
    public class GameChar : VirtualThingEntityBase
    {
        public GameChar()
        {
        }

        public GameChar(VirtualThing thing) : base(thing)
        {
        }

        #region 敏感信息

        [NotMapped]
        [JsonIgnore]
        public Guid CurrentToken { get; set; }

        #endregion 敏感信息

        /// <summary>
        /// 角色显示用的名字。就是昵称，不可重复。
        /// </summary>
        [MaxLength(64)]
        public string DisplayName { get => Thing.ExtraString; set => Thing.ExtraString = value; }

        /// <summary>
        /// 创建该对象的通用协调时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;


    }
}
