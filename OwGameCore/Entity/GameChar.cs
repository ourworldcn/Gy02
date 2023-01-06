using Microsoft.EntityFrameworkCore;
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
    /// <summary>
    /// TODO 标记强类型对象的模板Id。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TemplateIdAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tidString"></param>
        public TemplateIdAttribute(string tidString)
        {
            _TemplateId = Guid.Parse(tidString);

            // Implement code here

        }

        /// <summary>
        /// See the attribute guidelines at 
        /// http://go.microsoft.com/fwlink/?LinkId=85236
        /// </summary>
        readonly Guid _TemplateId;
        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TemplateId
        {
            get { return _TemplateId; }
        }

    }

    /// <summary>
    /// 游戏角色类。
    /// </summary>
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

        #region 普通属性

        /// <summary>
        /// 角色显示用的名字。就是昵称，不可重复。
        /// </summary>
        [MaxLength(64)]
        [JsonIgnore]
        public Guid UserId { get => Guid.TryParse(Thing.ExtraString, out var id) ? id : Guid.Empty; set => Thing.ExtraString = value.ToString(); }

        /// <summary>
        /// 创建该对象的通用协调时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        #endregion 普通属性

    }
}
