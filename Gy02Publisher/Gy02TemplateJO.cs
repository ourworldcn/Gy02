using GY02.Publisher;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GY02.Templates
{
    #region Udp相关

    /// <summary>
    /// 表示类是一个需要udp解码的类。
    /// </summary>
    public interface IJsonData
    {

    }

    /// <summary>
    /// Udp通知数据类。在侦听成功后会收到一次该数据。
    /// </summary>
    [Guid("24C3FEAA-4CF7-49DC-9C1E-36EBB92CCD12")]
    public class ListenStartedDto : IJsonData
    {
        /// <summary>
        /// 客户端登录的Token。
        /// </summary>
        public Guid Token { get; set; }

        /// <summary>
        /// 服务器所见的客户端地址。
        /// </summary>
        public string IPEndpoint { get; set; }
    }
    #endregion Udp相关

    #region 商城相关

    /// <summary>
    /// 
    /// </summary>
    public partial class GameShoppingItem
    {
        /// <summary>
        /// 该商品项的游戏周期。
        /// </summary>
        public GamePeriod Period { get; set; } = new GamePeriod();

        /// <summary>
        /// 组号。对同一个"页签"内的项进行分组。
        /// </summary>
        public int? GroupNumber { get; set; }

        /// <summary>
        /// 在一个购买周期内最多购买数量。
        /// </summary>
        /// <value>省略或null则不限制一个周期内最大购买数量。</value>
        public decimal? MaxCount { get; set; }

        /// <summary>
        /// 购买需要的代价。
        /// </summary>
        public List<BlueprintInItem> Ins { get; set; } = new List<BlueprintInItem>();

        /// <summary>
        /// 获得的物品。
        /// </summary>
        public List<BlueprintOutItem> Outs { get; set; } = new List<BlueprintOutItem>();
    }

    /// <summary>
    /// 定义周期的类。
    /// </summary>
    public class GamePeriod
    {
        /// <summary>
        /// 开始时间。
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// 结束时间。
        /// </summary>
        public DateTime? End { get; set; }

        /// <summary>
        /// 循环周期长度字符串表示。
        /// 如1m，1y分别表示一月和一年，其中一些是不确定时长度的间间隔，但在实际应用中却常有需求。支持：s秒，d天，w周，m月，y年。
        /// </summary>
        public string PeriodString { get; set; }

        /// <summary>
        /// 有效周期长度字符串表示。
        /// 如1m，1y分别表示一月和一年，其中一些是不确定时长度的间间隔，但在实际应用中却常有需求。支持：s秒，d天，w周，m月，y年。
        /// </summary>
        public string ValidPeriodString { get; set; }

#if NETCOREAPP3_0_OR_GREATER

        /// <summary>
        /// 循环周期长度。
        /// </summary>
        [JsonIgnore]
        public TimeSpanEx Period => new TimeSpanEx(PeriodString);

        /// <summary>
        /// 有效周期长度。
        /// </summary>
        [JsonIgnore]
        public TimeSpanEx ValidPeriod => new TimeSpanEx(ValidPeriodString);

        /// <summary>
        /// 获取指定时间点是否在该对象标识的有效期内。
        /// </summary>
        /// <param name="nowUtc"></param>
        /// <param name="start">返回true时这里返回<paramref name="nowUtc"/>时间点所处周期的起始时间点。其它情况此值是随机值。</param>
        /// <returns></returns>
        public bool IsValid(DateTime nowUtc, out DateTime start)
        {
            start = Start;
            if (nowUtc > End)   //若已经超期
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_INVALID_DATA);
                OwHelper.SetLastErrorMessage($"指定的时间{nowUtc}商品项最终有效期{End.Value}。");
                return false;
            }
            while (true)    //TODO 需要提高性能
            {
                if (start > nowUtc) //若已经超期
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_INVALID_DATA);
                    OwHelper.SetLastErrorMessage($"指定的时间{nowUtc}不在商品有效期内。");
                    return false;
                }
                if (start + ValidPeriod > nowUtc)    //若找到合适的项
                {
                    break;
                }
                start += Period;
            }
            return true;
        }
#endif
    }

    #endregion 商城相关


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

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 初始数量。
        /// </summary>
        public decimal Count { get; set; }
        #endregion 基础数据

        #region 商城相关

        /// <summary>
        /// 商城配置项。
        /// </summary>
        public GameShoppingItem ShoppingItem { get; set; }

        #endregion 商城相关

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

        /// <summary>
        /// 快速变化属性的字典集合，键是属性名，值快速变化属性的对象。
        /// </summary>
        public Dictionary<string, FastChangingProperty> Fcps { get; set; } = new Dictionary<string, FastChangingProperty>();

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

    /// <summary>
    /// 变化数据的封装类。
    /// </summary>
    public class FastChangingProperty : ICloneable
    {
        /// <summary>
        /// 
        /// </summary>
        public FastChangingProperty()
        {

        }

        /// <summary>
        /// 构造函数、
        /// </summary>
        /// <param name="delay">计算间隔。</param>
        /// <param name="increment">增量。</param>
        /// <param name="maxVal">最大值。不会超过此值。</param>
        /// <param name="currentVal">当前值。</param>
        /// <param name="lastComputerDateTime">时间。建议一律采用Utc时间。</param>
        public FastChangingProperty(TimeSpan delay, decimal increment, decimal maxVal, decimal currentVal, DateTime lastComputerDateTime)
        {
            if (increment > 0 && maxVal < currentVal || increment < 0 && maxVal > currentVal)  //若不向终值收敛
                Debug.WriteLine("不向终值收敛。");
            _CurrentValue = currentVal;
            LastDateTime = lastComputerDateTime;
            Delay = delay;
            StepValue = increment;
        }

        #region 属性及相关

        /// <summary>
        /// 最小值。
        /// </summary>
        public decimal MinValue { get; set; }

        /// <summary>
        /// 最大值。
        /// </summary>
        public decimal MaxValue { get; set; }

        /// <summary>
        /// 每次跳点的增量。可能是正值也可能是负值，正值每次跳变时增加<see cref="CurrentValue"/>，否则减少。
        /// </summary>
        public decimal StepValue { get; set; }

        /// <summary>
        /// 多久计算一次。即每次跳变的时间长度。
        /// </summary>
        public TimeSpan Delay { get; set; }

        private decimal _CurrentValue;
        /// <summary>
        /// 获取或设置最后计算的结果。<see cref="LastDateTime"/>这个时点上计算的值。
        /// </summary>
        public decimal CurrentValue { get => _CurrentValue; set => _CurrentValue = value; }

        /// <summary>
        /// 获取或设置最后计算的时间。建议一律采用Utc时间。默认值是构造时的当前时间。
        /// </summary>
        /// <value>默认值：<see cref="DateTime.UtcNow"/></value>
        public DateTime LastDateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 一个记录额外信息的属性。本类成员不使用该属性。
        /// </summary>
        public object Tag { get; set; }

        #endregion 属性及相关

#if NETCOREAPP2_1_OR_GREATER
        /// <summary>
        /// 获取当前值。自动修改<see cref="LastDateTime"/>和<see cref="CurrentValue"/>属性。
        /// </summary>
        /// <param name="now">当前时间。返回时可能更改，如果没有正好到跳变时间，则会提前到上一次跳变的时间点。</param>
        /// <returns>更改后的值(<see cref="CurrentValue"/>)。</returns>
        public decimal GetCurrentValue(ref DateTime now)
        {
            var count = (long)Math.Round((decimal)(now - LastDateTime).Ticks / Delay.Ticks, MidpointRounding.ToNegativeInfinity);   //跳变次数,回调可能多跳一次

            LastDateTime += Delay * count;
            now = LastDateTime;
            if (StepValue > 0) //若增量跳变
            {
                if (_CurrentValue >= MaxValue)  //若已经结束
                {
                    return _CurrentValue;
                }
                else //若尚未结束
                {
                    _CurrentValue = Math.Clamp(_CurrentValue + count * StepValue, MinValue, MaxValue);
                }
            }
            else if (StepValue < 0) //若减量跳变
            {
                if (_CurrentValue <= MinValue)  //若已经结束
                {
                    return _CurrentValue;
                }
                else //若尚未结束
                {
                    _CurrentValue = Math.Clamp(_CurrentValue + count * StepValue, MinValue, MaxValue);
                }
            }
            else
                throw new InvalidOperationException("步进值不能是0。");
            return _CurrentValue;
        }

        /// <summary>
        /// 使用当前Utc时间获取当前值。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal GetCurrentValueWithUtc()
        {
            DateTime now = DateTime.UtcNow;
            return GetCurrentValue(ref now);
        }

        #region IValidatableObject接口相关

        /// <summary>
        /// 
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return default;
        }

        #endregion IValidatableObject接口相关

#endif

        #region 事件及相关

        /// <summary>
        /// 设置最后计算得到的值，同时将计算时间更新到最接近指定点的时间。
        /// </summary>
        /// <param name="val">这个时间点不晚于指定时间点，且又是正好一跳的时间点。</param>
        /// <param name="dateTime"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLastValue(decimal val, ref DateTime dateTime)
        {
            var remainder = (dateTime - LastDateTime).Ticks % Delay.Ticks;
            LastDateTime = dateTime.AddTicks(-remainder);
            if (LastDateTime > dateTime)    //若时间点超过指定值
                LastDateTime -= Delay;
            dateTime = LastDateTime;
            _CurrentValue = val;
        }

        /// <summary>
        /// 深度克隆该对象。
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var result = new FastChangingProperty
            {
                CurrentValue = _CurrentValue,
                Delay = Delay,
                LastDateTime = LastDateTime,
                MaxValue = MaxValue,
                MinValue = MinValue,
                StepValue = StepValue,
                Tag = Tag,
            };
            return result;
        }

        #endregion 事件及相关

    }
}
