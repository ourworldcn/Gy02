﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        public int? Gid { get; set; }

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
                if (result is TemplateStringFullView t)
                    t._RawTemplate = this;
                return result;
            }
            catch (Exception err)
            {
                Debug.WriteLine($"{PropertiesString}{Environment.NewLine}{err.Message}");
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
            //TODO
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

        int? _Gid;
        /// <summary>
        /// 分类号。
        /// </summary>
        public int? Gid
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

        /// <summary>
        /// 获取大类号。
        /// </summary>
        /// <returns></returns>
        public int? GetCatalog1() => Gid.HasValue ? new int?(Gid.Value / 10000000) : null;

        /// <summary>
        /// 获取中类号。
        /// </summary>
        /// <returns></returns>
        public int? GetCatalog2() => Gid.HasValue ? new int?(Gid.Value / 100000 % 100) : null;

        /// <summary>
        /// 获取小类号。
        /// </summary>
        /// <returns></returns>
        public int? GetCatalog3() => Gid.HasValue ? new int?(Gid.Value / 1000 % 100) : null;

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
        /// 实体类型的Guid。
        /// </summary>
        public Guid TypeGuid { get; set; }

        /// <summary>
        /// 泛型参数类型的GUID。
        /// 若TypeGuid不是泛型则应保持为null。
        /// </summary>
        public Guid? SubTypeGuid { get; set; }

        /// <summary>
        /// 默认的父容器模板Id。
        /// </summary>
        [JsonPropertyName("ptid")]
        public Guid ParentTId { get; set; }

        /// <summary>
        /// 容量。
        /// </summary>
        /// <value>默认值：-1。</value>
        [JsonPropertyName("cap")]
        public decimal Capacity { get; set; } = -1;

        /// <summary>
        /// 最大堆叠数。不可堆叠物该属性为1。-1表示不限制。
        /// </summary>
        /// <value>默认值：1</value>
        [JsonPropertyName("stk")]
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
        [JsonPropertyName("pow")]
        public decimal[] Pow { get; set; }

        /// <summary>
        /// 暴击率。
        /// </summary>
        [JsonPropertyName("crit_pct")]
        public decimal CritPct { get; set; }

        /// <summary>
        /// 暴击倍数。1表示暴击和普通伤害一致。
        /// </summary>
        [JsonPropertyName("crit")]
        public decimal Crit { get; set; }

        /// <summary>
        /// 词条的集合。
        /// </summary>
        [JsonPropertyName("p_skills")]
        public TemplateSkillItem[] Skills { get; set; }

        #endregion 装备数据

        #region 升级相关

        /// <summary>
        /// 升级使用配方的TId。不能升级的这里可能是null。
        /// </summary>
        public Guid? LvUpTId { get; set; }

        List<CostInfo> _LvUpData;
        /// <summary>
        /// 对于升级公式这里是升级公式的内容，对于其它类型的数据，这里是空集合。
        /// </summary>
        public List<CostInfo> LvUpData { get => _LvUpData ?? (_LvUpData = new List<CostInfo>()); set => _LvUpData = value; }

        #endregion 升级相关

        #region 合成相关

        /// <summary>
        /// 产出物的集合。当前版本该集合中只有一项。
        /// </summary>
        public List<BlueprintOutItem> Out { get; set; } = new List<BlueprintOutItem>();

        /// <summary>
        /// 材料的集合。
        /// </summary>
        public List<BlueprintInItem> In { get; set; } = new List<BlueprintInItem>();

        /// <summary>
        /// 合成公式的Id。省略则没有合成公式。
        /// </summary>
        public Guid? CompositeId { get; set; }
        #endregion 合成相关

        #region 战斗相关
        /// <summary>
        /// 关卡入场费。
        /// </summary>
        public List<BlueprintInItem> EntranceFees { get; set; } = new List<BlueprintInItem>();

        #endregion

        #region 孵化相关

        /// <summary>
        /// 孵化数据。
        /// </summary>
        public FuhuaInfo Fuhua { get; set; }

        #endregion 孵化相关

        #region 骰子相关

        /// <summary>
        /// 池子的数据。
        /// </summary>
        public GameDice Dice { get; set; }

        #endregion 骰子相关

        private string GetDebuggerDisplay()
        {
            return $"{ToString()}({DisplayName})";
        }

        /// <summary>
        /// 是否可以堆叠。
        /// </summary>
        /// <returns>true可堆叠，false不可堆叠。</returns>
        public bool IsStk()
        {
            return Stk != decimal.One;
        }


    }

    /// <summary>
    /// 
    /// </summary>
    public class FuhuaInfo
    {
        /// <summary>
        /// 双亲1的条件。
        /// </summary>
        public GameThingPrecondition Parent1Conditional { get; set; }

        /// <summary>
        /// 双亲2的条件。
        /// </summary>
        public GameThingPrecondition Parent2Conditional { get; set; }

        /// <summary>
        /// 卡池1的。
        /// </summary>
        public Guid DiceTId1 { get; set; }

        /// <summary>
        /// 卡池2的。
        /// </summary>
        public Guid DiceTId2 { get; set; }

        /// <summary>
        /// 消耗物的集合。
        /// </summary>
        public List<BlueprintInItem> In { get; set; } = new List<BlueprintInItem>();


    }

    /// <summary>
    /// 升级的代价类。
    /// </summary>
    public partial class CostInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CostInfo()
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
        public List<decimal> Counts { get; set; } = new List<decimal>();
    }

    /// <summary>
    /// 定位一个物品的结构。
    /// </summary>
    public partial class GameThingPrecondition : Collection<GameThingPreconditionItem>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameThingPrecondition()
        {

        }
    }

    /// <summary>
    /// 定位一个物品的条件的详细项。如果指定多种属性过滤则需要满足所有属性要求。
    /// </summary>
    public partial class GameThingPreconditionItem
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameThingPreconditionItem()
        {

        }

        /// <summary>
        /// 容器的模板Id。省略则不限制。
        /// </summary>
        public Guid? ParentTId { get; set; }

        /// <summary>
        /// 需要包含的属名称（如果有多项则必须全部包含）。空集合则不限制。
        /// </summary>
        public List<string> Genus { get; set; } = new List<string>();

        /// <summary>
        /// 物品的TId。省略则不限制。
        /// </summary>
        public Guid? TId { get; set; }

        /// <summary>
        /// 要求的最小数量。省略则不限制。
        /// </summary>
        public decimal? MinCount { get; set; }

        private List<GeneralConditionalItem> _Contional;

        /// <summary>
        /// 扩展条件，针对属性的不等式。有多项时需要同时都符合才认为命中。
        /// </summary>
        public List<GeneralConditionalItem> GeneralConditional
        {
            get { return _Contional ?? (_Contional = new List<GeneralConditionalItem>()); }
            set { _Contional = value; }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString();
        }
    }

    /// <summary>
    /// 通用属性条件项。
    /// </summary>
    public class GeneralConditionalItem
    {
        /// <summary>
        /// 操作符。暂定支持六种关系运算,如 &lt;= 等。
        /// </summary>
        [JsonPropertyName("op")]
        public string Operator
        {
            get; set;
        }

        /// <summary>
        /// 通常是属性名。
        /// </summary>
        [JsonPropertyName("lo")]
        public object LeftOperand
        {
            get; set;
        }

        /// <summary>
        /// 一般是一个数字，用于和属性对比。
        /// </summary>
        [JsonPropertyName("ro")]
        public object RightOperand
        {
            get; set;
        }
    }

    #region 合成相关

    /// <summary>
    /// 合成的材料信息。
    /// </summary>
    public class BlueprintInItem
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public BlueprintInItem()
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

        /// <summary>
        /// 与主材料共有类属。
        /// </summary>
        public List<string> Genus { get; set; }
    }

    /// <summary>
    /// 蓝图产出项数据结构。
    /// </summary>
    public class BlueprintOutItem
    {
        /// <summary>
        /// 当此物品合成创建时应放入的父容器的模板Id。省略则将物品放入默认容器。
        /// </summary>
        public Guid? ParentTId { get; set; }

        /// <summary>
        /// 新产出物品的模板Id。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 新产出物品的数量。对非堆叠物一定是1。
        /// </summary>
        public decimal Count { get; set; }

    }

    /// <summary>
    /// 蓝图输入项数据结构。
    /// </summary>
    public class BlueprintInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public BlueprintInfo()
        {

        }

        /// <summary>
        /// 产出物的集合。
        /// </summary>
        public List<BlueprintOutItem> Out { get; set; } = new List<BlueprintOutItem>();

        /// <summary>
        /// 材料的集合。
        /// </summary>
        public List<BlueprintInItem> In { get; set; } = new List<BlueprintInItem>();

    }

    #endregion 合成相关

    #region 孵化相关

    #endregion 孵化相关

    #region 骰子相关

    /// <summary>
    /// 定义一个"池子"
    /// </summary>
    public class GameDice
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameDice()
        {

        }

        /// <summary>
        /// 指定该"池子"内输出的最大项数。
        /// </summary>
        /// <value>应大于0。</value>
        public int MaxCount { get; set; }

        /// <summary>
        /// 允许生成重复项。
        /// </summary>
        /// <value>默认值：false 不允许。</value>
        public bool AllowRepetition { get; set; }

        /// <summary>
        /// 每个投骰子的项。
        /// </summary>
        public List<GameDiceItem> Items { get; set; } = new List<GameDiceItem>();

    }

    /// <summary>
    /// 池子项。
    /// </summary>
    public class GameDiceItem
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameDiceItem()
        {

        }

        /// <summary>
        /// 池子物品的条件。
        /// </summary>
        public GameThingPrecondition Precondition { get; set; } = new GameThingPrecondition();

        /// <summary>
        /// 权重值。在同一个池子中所有项加起来的权重是分母。
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// 需要此项有效的前置条件，这个条件针对角色数据来设置。
        /// </summary>
        public GameThingPrecondition GuardConditions { get; set; } = new GameThingPrecondition();

        (Guid, decimal, decimal)? _Summary;
        /// <summary>
        /// 获取产出的(模板Id,数量,权重)。
        /// </summary>
        /// <returns></returns>
        public (Guid, decimal, decimal) GetSummary()
        {
            if (!_Summary.HasValue)
            {
                var item = Precondition.FirstOrDefault(c => c.TId.HasValue);
                _Summary = (item?.TId ?? Guid.Empty, item?.MinCount ?? 0, Weight);
            }
            return _Summary.Value;
        }
    }

    #endregion 骰子相关

}
