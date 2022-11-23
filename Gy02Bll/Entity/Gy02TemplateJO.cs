using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Gy02Bll.Entity
{
    public class GameProperties
    {

    }

    /// <summary>
    /// 模板对象Json的数据。
    /// </summary>
    public class Gy02TemplateJO : TemplateJO
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
