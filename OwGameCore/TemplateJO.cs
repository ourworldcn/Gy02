using OW.Game.Conditional;
using System;
using System.Collections.Generic;

namespace Gy02Bll.Entity
{
    /// <summary>
    /// 升级时增长的属性。
    /// </summary>
    public class LuInfo
    {
        public Dictionary<string, decimal[]> DecimalProperties { get; set; } = new Dictionary<string, decimal[]>();

        public List<GamePropertyCondition> Cost { get; set; }
    }

    public class TemplateJO
    {
        public TemplateJO()
        {

        }

        /// <summary>
        /// 所属的属。
        /// </summary>
        public List<string> Genus { get; set; } = new List<string>();

        /// <summary>
        /// 创建时自带的孩子。
        /// </summary>
        public List<Guid> ChildrenTIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 升级时使用的序列数值属性。
        /// </summary>
        public LuInfo LuInfo { get; set; } = new LuInfo();
    }
}