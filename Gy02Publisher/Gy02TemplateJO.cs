using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Gy02Bll.Templates
{
    /// <summary>
    /// 原始的的模板类。
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public partial class RawTemplate
    {
        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 显示名。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 分类号。
        /// </summary>
        public decimal? Gid { get; set; }

        /// <summary>
        /// 属性字符串，Json格式。
        /// </summary>
        public string PropertiesString { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        public string Remark { get; set; }

#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// 获取<see cref="PropertiesString"/>的Json对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetJsonObject<T>()
        {
            try
            {
                var result = JsonSerializer.Deserialize<T>(PropertiesString);
                var tmp = result as TemplateStringFullView;
                if (tmp != null)
                    tmp._RawTemplate = this;
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string GetDebuggerDisplay()
        {
            return DisplayName;
        }
#endif //NETCOREAPP3_0_OR_GREATER
    }

    /// <summary>
    /// 词条的数据结构。
    /// </summary>
    public class TemplateSkillItem
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public TemplateSkillItem()
        {

        }

        /// <summary>
        /// 词条的Id。
        /// </summary>
        [JsonPropertyName("skillsid")]
        public Guid SkillsId { get; set; }

        /// <summary>
        /// 词条是否生效。
        /// </summary>
        [JsonPropertyName("enable")]
        public bool Enable { get; set; }
    }

    /// <summary>
    /// 模板的完整视图。
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public partial class TemplateStringFullView
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public TemplateStringFullView()
        {
        }

        #region 本体数据

        Guid _TemplateId;
        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TemplateId
        {
            get
            {
                return _RawTemplate?.Id ?? _TemplateId;
            }
            set => _TemplateId = value;
        }

        string _DisplayName;
        /// <summary>
        /// 显示名。
        /// </summary>
        public string DisplayName
        {
            get
            {
                return _RawTemplate?.DisplayName ?? _DisplayName;
            }
            set => _DisplayName = value;
        }

        decimal? _Gid;
        /// <summary>
        /// 分类号。
        /// </summary>
        public decimal? Gid
        {
            get
            {
                return _RawTemplate?.Gid ?? _Gid;
            }
            set => _Gid = value;
        }

        string _Remark;
        /// <summary>
        /// 备注。
        /// </summary>
        public string Remark
        {
            get
            {
                return _RawTemplate?.Remark ?? _Remark;
            }
            set => _Remark = value;
        }

        #endregion 本体数据

        #region 基础数据

        [JsonIgnore]
        internal RawTemplate _RawTemplate;

        /// <summary>
        /// 存储额外的非强类型属性数据。
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> ExtraProperties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 类型的Guid。
        /// </summary>
        public Guid TypeGuid { get; set; }

        /// <summary>
        /// 泛型参数类型的GUID。
        /// 若TypeGuid不是泛型则应保持为null。
        /// </summary>
        public Guid? SubTypeGuid { get; set; }

        /// <summary>
        /// 父容器模板Id。
        /// </summary>
        [JsonPropertyName("ptid")]
        public Guid ParentTId { get; set; }

        /// <summary>
        /// 容量。
        /// </summary>
        /// <value>默认值：0</value>
        [JsonPropertyName("cap")]
        public decimal Capacity { get; set; }

        /// <summary>
        /// 最大堆叠数。
        /// </summary>
        /// <value>默认值：1</value>
        public decimal Stk { get; set; } = decimal.One;

        /// <summary>
        /// 创建时要一同创建的子对象的模板Id。
        /// </summary>
        public Guid[] TIdsOfCreate { get; set; }

        /// <summary>
        /// 为true时，数量为0时也不会删除该物品，省略或为false则删除。
        /// </summary>
        public bool Count0Reserved { get; set; }

        /// <summary>
        /// 类属字符串集合。
        /// </summary>
        [JsonPropertyName("genus")]
        public string[] Genus { get; set; }

        #endregion 基础数据

        #region 装备数据
        /// <summary>
        /// 攻击数值序列。
        /// </summary>
        [JsonPropertyName("atk")]
        public decimal[] Atk { get; set; }

        /// <summary>
        /// 防御数值序列。
        /// </summary>
        [JsonPropertyName("def")]
        public decimal[] Def { get; set; }

        /// <summary>
        /// 力量属性数值序列。
        /// </summary>
        [JsonPropertyName("pwo")]
        public decimal[] Pwo { get; set; }

        /// <summary>
        /// 词条的集合。
        /// </summary>
        [JsonPropertyName("p_skills")]
        public TemplateSkillItem[] Skills { get; set; }

        #endregion 装备数据

        private string GetDebuggerDisplay()
        {
            return DisplayName;
        }

    }

    /// <summary>
    /// 创建对象时的行为信息。
    /// </summary>
    public class CreateTInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CreateTInfo()
        {

        }

        /// <summary>
        /// 创建时自带的孩子的模板Id集合。
        /// </summary>
        public List<Guid> ChildrenTIds { get; set; } = new List<Guid>();

    }

    /// <summary>
    /// 升级的代价类。
    /// </summary>
    public class CostTInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CostTInfo()
        {
        }

        /// <summary>
        /// 选取物品的条件。
        /// </summary>
        ///public GameThingPrecondition Conditional { get; set; } = new GameThingPrecondition();

        /// <summary>
        /// 消耗的数量。第一个值是由0级升级到1级这个动作的消耗数量。
        /// 注意消耗数量可能是0，代表需要此物品但不消耗此物品。若是null或空则表示所有级别都不消耗。
        /// </summary>
        public List<decimal> Counts { get; set; }
    }

    /// <summary>
    /// 升级时增长的属性。
    /// </summary>
    public class UpgradeTInfo
    {
        /// <summary>
        /// 每集的数值。键是属性的名，值每级别对应数值的数组。
        /// </summary>
        public Dictionary<string, decimal[]> DecimalProperties { get; set; } = new Dictionary<string, decimal[]>();

        /// <summary>
        /// 升级对应的代价。
        /// </summary>
        public List<CostTInfo> Cost { get; set; } = new List<CostTInfo>();
    }

    /// <summary>
    /// 合成的材料信息。
    /// </summary>
    public class CompositingTInfoItem
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CompositingTInfoItem()
        {

        }

        /// <summary>
        /// 选取物品的条件。
        /// </summary>
        ///public GameThingPrecondition Conditional { get; set; } = new GameThingPrecondition();

        /// <summary>
        /// 消耗的数量。
        /// 注意消耗数量可能是0，代表需要此物品但不消耗此物品。
        /// </summary>
        public decimal Count { get; set; }
    }

    /// <summary>
    /// 合成物品行为的定义数据。
    /// </summary>
    public class CompositingTInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CompositingTInfo()
        {

        }

        /// <summary>
        /// 当此物品合成创建时应放入的父容器的模板Id。
        /// </summary>
        public Guid CompositingPTId { get; set; }

        /// <summary>
        /// 合成时材料的集合。
        /// </summary>
        public List<CompositingTInfoItem> Items { get; set; } = new List<CompositingTInfoItem>();
    }

}
