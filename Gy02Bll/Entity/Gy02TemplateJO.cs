using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gy02Bll.Entity
{
    /// <summary>
    /// 创建时需要初始及携带的子对象。
    /// </summary>
    public class CreateInfo
    {
        public List<Guid> ChildrenTIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 升级时增长的属性。
    /// </summary>
    public class LvUpInfo
    {
        public Dictionary<string, decimal[]> DecimalProperties { get; set; } = new Dictionary<string, decimal[]>();
    }

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
        /// 初始创建时的孩子对象TId。
        /// </summary>
        public CreateInfo CreateInfo { get; set; } = new CreateInfo();

        /// <summary>
        /// 升级时使用的序列数值属性。
        /// </summary>
        public LvUpInfo LvUpInfo { get; set; } = new LvUpInfo();

        /// <summary>
        /// 剩余的扩展属性。
        /// </summary>
        public Dictionary<string, object> ExtraProperties { get; set; } = new Dictionary<string, object>();

    }
}
