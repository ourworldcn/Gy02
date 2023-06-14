using GY02.Templates;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OW.Game.Entity
{
    /// <summary>
    /// 邮件对象。
    /// </summary>
    [Guid("0465A794-42E7-4B14-AE8B-2BF282FAA0BE")]  //宿主在VirtualThing中
    public partial class GameMail : GameEntityBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameMail()
        {

        }

        #region 基本属性

        /// <summary>
        /// 邮件标题。
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// 邮件正文。
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// 附件集合。
        /// </summary>
        public List<GameEntitySummary> Attachment { get; set; } = new List<GameEntitySummary> { };

        /// <summary>
        /// 发件人。可以不填写，自动用Token身份填入。
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// 收件人。通常这里是收件角色Id的<see cref="Guid.ToString()"/>字符串形式，这样可以用于快速查找指定角色的所有邮件。
        /// </summary>
        [JsonIgnore]
        public string To { get => ((VirtualThing)Thing).ExtraString; set => ((VirtualThing)Thing).ExtraString = value; }

        /// <summary>
        /// 存储一些特殊属性的字典。
        /// </summary>
        public Dictionary<string, string> Dictionary1 { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 存储一些特殊属性的字典。
        /// </summary>
        public Dictionary<string, string> Dictionary2 { get; set; } = new Dictionary<string, string>();

        #endregion 基本属性

        #region 动态属性

        /// <summary>
        /// 已读的时间，null标识未读。
        /// </summary>
        public DateTime? ReadUtc { get; set; }

        /// <summary>
        /// 发件日期。
        /// </summary>
        public DateTime SendUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 领取附件的日期，null标识尚未领取。
        /// </summary>
        public DateTime? PickUpUtc { get; set; }

        #endregion 动态属性

    }
}
