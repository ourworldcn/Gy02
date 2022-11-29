using OW.Game.Conditional;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Gy02Bll.Templates
{
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
        public GameThingPrecondition Conditional { get; set; } = new GameThingPrecondition();

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
    /// 使用该物品时行为定义数据。
    /// 该功能尚未实装。
    /// </summary>
    public class UseTInfo
    {

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
        public GameThingPrecondition Conditional { get; set; } = new GameThingPrecondition();

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

    /// <summary>
    /// 
    /// </summary>
    public class TemplateJsonObjectBase
    {
        private CompositingTInfo compositingInfo = new CompositingTInfo();

        /// <summary>
        /// 构造函数。
        /// </summary>
        public TemplateJsonObjectBase()
        {

        }

        /// <summary>
        /// 模板的唯一Id。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 长整型的一个数字，可以用于分类，但服务器不使用。
        /// </summary>
        public long ExtraLong { get; set; }

        /// <summary>
        /// 所属的属。服务器后续复杂的计算系统使用该属。
        /// </summary>
        public List<string> Genus { get; set; } = new List<string>();

        /// <summary>
        /// <seealso cref="CreateTInfo"/>。
        /// </summary>
        public CreateTInfo CreateInfo { get; set; } = new CreateTInfo();

        /// <summary>
        /// 升级时使用的序列数值属性。
        /// 该属性内容不会直接复制到对象，需要按等级计算得到唯一值复制到对象。
        /// </summary>
        public UpgradeTInfo UpgradeInfo { get; set; } = new UpgradeTInfo();

        /// <summary>
        /// 合成此物品的材料信息。
        /// </summary>
        public CompositingTInfo CompositingInfo { get => compositingInfo ?? (compositingInfo = new CompositingTInfo()); set => compositingInfo = value; }

        /// <summary>
        /// <seealso cref="UseTInfo"/>。
        /// </summary>
        public UseTInfo UseInfo { get; set; } = new UseTInfo();
    }

    /// <summary>
    /// 模板对象Json的数据。
    /// 注意此类可能不断添加一些成员。
    /// </summary>
    public class Gy02TemplateJO : TemplateJsonObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public Gy02TemplateJO()
        {

        }

        /// <summary>
        /// 未明确添加的属性，即剩余的扩展属性。
        /// </summary>
        [JsonExtensionData] //该批注保证没有明确添加的属性被反序列化到此字典内，键是属性名，但值根据不同的反序列化程序可能完全不同。
        public Dictionary<string, object> ExtraProperties { get; set; } = new Dictionary<string, object>();

    }
}
