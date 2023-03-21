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
    public class RawTemplate
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
            return JsonSerializer.Deserialize<T>(PropertiesString);
        }
#endif //NETCOREAPP3_0_OR_GREATER
    }

    /// <summary>
    /// 模板属性字符串的基础类。
    /// </summary>
    public class TemplatePropertiesStringBase
    {
        /// <summary>
        /// 
        /// </summary>
        public TemplatePropertiesStringBase()
        {

        }

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

        Dictionary<string, object> _ExtraProperties;
        /// <summary>
        /// 未能明确解析的字段放在此属性内。
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> ExtraProperties
        {
            get
            {
                if (_ExtraProperties is null)
                    Interlocked.CompareExchange(ref _ExtraProperties, new Dictionary<string, object>(), null);
                return _ExtraProperties;
            }
            set { _ExtraProperties = value; }
        }
    }

    /// <summary>
    /// 模板属性字符串的主类。
    /// 用于解析<see cref="RawTemplate.PropertiesString"/>属性的类型。
    /// </summary>
    public class TemplatePropertiesString : TemplatePropertiesStringBase
    {
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
        public Guid SkillsId { get; set; }

        /// <summary>
        /// 词条是否生效。
        /// </summary>
        public bool Enable { get; set; }
    }

    /// <summary>
    /// 装备模板特有数据。
    /// </summary>
    public class EquipmentTemplatePropertiesString : TemplatePropertiesString
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public EquipmentTemplatePropertiesString()
        {

        }

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
    }

    /// <summary>
    /// 模板的完整视图。
    /// </summary>
    public class TemplateStringFullView
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public TemplateStringFullView()
        {

        }

        #region 基础数据
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
    }

    /// <summary>
    /// 模板基类。
    /// </summary>
    public class GameTemplateBase<T> where T : TemplatePropertiesStringBase
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
        /// 备注。
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 扩展属性封装对象。
        /// </summary>
        public T ExtraProperties { get; set; }
    }

    /// <summary>
    /// 模板主类。
    /// </summary>
    public class GameTemplate<T> : GameTemplateBase<T> where T : TemplatePropertiesStringBase
    {
        /// <summary>
        /// 从原始数据转换。
        /// </summary>
        /// <param name="raw"></param>
        public static implicit operator GameTemplate<T>(RawTemplate raw)
        {
            TemplatePropertiesString ts;
            try
            {
                ts = raw.GetJsonObject<TemplatePropertiesString>();
                //var tmp = raw.GetJsonObject<EquipmentTemplatePropertiesString>();
            }
            catch (Exception)
            {
                throw;
            }
            var result = new GameTemplate<T>()
            {
                DisplayName = raw.DisplayName,
                Gid = raw.Gid,
                Id = raw.Id,
                Remark = raw.Remark
            };
            result.ExtraProperties = ts as T;
            return result;
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
