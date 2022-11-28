using OW.Game.Conditional;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Gy02Bll.Template
{
    public class GameProperties
    {

    }

    /// <summary>
    /// 升级时增长的属性。
    /// </summary>
    public class LuInfo
    {
        public Dictionary<string, decimal[]> DecimalProperties { get; set; } = new Dictionary<string, decimal[]>();

        public List<GamePropertyCondition> Cost { get; set; }
    }

    public class TemplateJoBase
    {
        public TemplateJoBase()
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

    /// <summary>
    /// 模板对象Json的数据。
    /// </summary>
    public class Gy02TemplateJO : TemplateJoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public Gy02TemplateJO()
        {

        }

        /// <summary>
        /// 剩余的扩展属性。
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> ExtraProperties { get; set; } = new Dictionary<string, object>();

    }
}
