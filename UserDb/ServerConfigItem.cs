using System.ComponentModel.DataAnnotations;

namespace OW.Game.Store
{
    public class ServerConfigItem
    {
        /// <summary>
        /// Key的名字。
        /// </summary>
        [MaxLength(64)]
        [Key]
        public string Name { get; set; }

        /// <summary>
        /// 内容。
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 最后修改的日期。使用游戏世界时间。
        /// </summary>
        public DateTime LastModifyUtc { get; set; } = OwHelper.WorldNow;
    }
}
