using GY02.Publisher;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OW.DDD;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace GY02.Templates
{
    #region 基础数据

    /// <summary>
    /// 实体摘要信息类。
    /// </summary>
    [DisplayName("实体摘要")]
    [Description("由 OutItem 改名而来。")]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class GameEntitySummary : IValidatableObject, ICloneable
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameEntitySummary()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="tId"></param>
        /// <param name="count">对可堆叠物可以是任何数量，对不可堆叠物只能是正整数。</param>
        public GameEntitySummary(Guid tId, decimal count)
        {
            TId = tId;
            Count = count;
        }

        /// <summary>
        /// 特定原因记录物品唯一Id，通常为null。
        /// </summary>
#if NET5_0_OR_GREATER
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#endif
        public Guid? Id { get; set; }

        /// <summary>
        /// 父容器模板Id，为null则放置在默认容器中。
        /// </summary>
#if NET5_0_OR_GREATER
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#endif
        public Guid? ParentTId { get; set; }

        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 数量。对可堆叠物可以是任何数量，对不可堆叠物只能是正整数。
        /// </summary>
#if NET5_0_OR_GREATER
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#endif
        public decimal Count { get; set; }

        Dictionary<string, decimal> _AddPropertyDictionary;
        /// <summary>
        /// 合并计算的数值属性。此属性将根据名称累加到已有的属性上。
        /// </summary>
#if NET5_0_OR_GREATER
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("AddPropDic")]    //为节约字节数
#endif
        public Dictionary<string, decimal> AddPropertyDictionary
        {
            get => LazyInitializer.EnsureInitialized(ref _AddPropertyDictionary);
            set => _AddPropertyDictionary = value;
        }

        Dictionary<string, object> _ExtraDictionary;
        /// <summary>
        /// 额外的一些属性。
        /// 这里只能支持数值，字符串，bool，日期等简单属性。
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> ExtraDictionary
        {
            get => LazyInitializer.EnsureInitialized(ref _ExtraDictionary);
            set => _ExtraDictionary = value;
        }

        /// <summary>
        /// 获取一个深表副本。
        /// </summary>
        /// <remarks>所有成员都是结构时，深表副本等价于浅表副本。</remarks>
        /// <returns></returns>
        public object Clone() => new GameEntitySummary
        {
            Count = Count,
            Id = Id,
            ParentTId = ParentTId,
            TId = TId,
            _AddPropertyDictionary = _AddPropertyDictionary is null ? null : new Dictionary<string, decimal>(_AddPropertyDictionary),
            _ExtraDictionary = _ExtraDictionary is null ? null : new Dictionary<string, object>(_ExtraDictionary),
        };

        private string GetDebuggerDisplay()
        {
            return $"Summary({TId},{Count})";
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }
    }

    /// <summary>
    /// 描述一组产出。
    /// </summary>
    public class SequenceGameEntitySummary
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SequenceGameEntitySummary()
        {

        }

        List<Guid> _TIds;
        /// <summary>
        /// 多个物品的TId集合。
        /// 可以填写的数量少于另外两个集合的数量，如果其它两集合更长，则取此集合最后一个。
        /// </summary>
        public List<Guid> TIds
        {
            get => _TIds ?? (_TIds = new List<Guid>());
            set
            {
                _TIds = value;
                _Count = null;
            }
        }

        List<decimal> _Counts;

        /// <summary>
        /// 多个物品的数量集合。
        /// 可以填写的数量少于另外两个集合的数量，如果其它两集合更长，则取此集合最后一个。
        /// </summary>
        public List<decimal> Counts
        {
            get => _Counts ?? (_Counts = new List<decimal>());
            set
            {
                _Counts = value;
                _Count = null;
            }
        }

        List<Guid?> _ParentTIds;
        /// <summary>
        /// 多个物品放入的父容器的TId集合。
        /// 可以填写的数量少于另外两个集合的数量，如果其它两集合更长，则取此集合最后一个。
        /// 若是空引用或空集合，则返回null。
        /// </summary>
        public List<Guid?> ParentTIds
        {
            get => _ParentTIds ?? (_ParentTIds = new List<Guid?>());
            set
            {
                _ParentTIds = value;
                _Count = null;
            }
        }

        private int? _Count;

        /// <summary>
        /// 三个集合的最大长度。
        /// </summary>
        [JsonIgnore]
        public int Count
        {
            get
            {
                if (_Count.HasValue) return _Count.Value;
                _Count = Math.Max(Math.Max(TIds.Count, Counts.Count), ParentTIds.Count);
                return _Count.Value;
            }
        }

        /// <summary>
        /// 获取指定索引处的实体描述对象。
        /// </summary>
        /// <param name="index">要大于或等于0。</param>
        /// <returns>返回相应索引的预览对象，若索引过大，则取对应集合的最后一项（不会因为索引过大引发异常）。</returns>
        /// <exception cref="IndexOutOfRangeException">索引超出范围。</exception>
        public GameEntitySummary GetItem(int index)
        {
            if (index < 0) throw new IndexOutOfRangeException();

            var result = new GameEntitySummary
            {
                Count = Counts.Count > index ? Counts[index] : Counts[Counts.Count - 1],
                TId = TIds.Count > index ? TIds[index] : TIds[TIds.Count - 1],
            };
            if (ParentTIds?.Count > 0)
                result.ParentTId = ParentTIds.Count > index ? ParentTIds[index] : ParentTIds[ParentTIds.Count - 1];
            return result;
        }
    }

    /// <summary>
    /// 动态产出/消耗。
    /// </summary>
    public class SequenceOut : IValidatableObject
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SequenceOut()
        {

        }

        /// <summary>
        /// 过滤并获取一个实体，该实体使用 <see cref="GetIndexExpression"/> 属性指定的方法提取索引值。
        /// </summary>
        public GameThingPreconditionItem[] Conditions { get; set; }

        /// <summary>
        /// 获取索引的对象。
        /// </summary>
        public GeneralConditionalItem GetIndexExpression { get; set; }

        /// <summary>
        /// 产出的集合。
        /// </summary>
        public SequenceGameEntitySummary Outs { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }
    }

    #endregion 基础数据

    #region 全服配置数据

    #endregion 全服配置数据

    #region Udp相关

    /// <summary>
    /// 表示类是一个需要udp解码的类。
    /// </summary>
    public interface IJsonData
    {
        /// <summary>
        /// 序号。
        /// </summary>
        long Seq { get; set; }
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

        /// <summary>
        /// 序号。
        /// </summary>
        public long Seq { get; set; }
    }

    /// <summary>
    /// 邮件到达的Udp通知数据类。
    /// </summary>
    [Guid("4E556F0D-8A46-4B7D-8A7B-2D24753DB68A")]
    public class MailArrivedDto : IJsonData
    {
        /// <summary>
        /// 序号。
        /// </summary>
        public long Seq { get; set; }

        /// <summary>
        /// 新到达邮件的唯一Id集合。
        /// </summary>
        public List<Guid> MailIds { get; set; } = new List<Guid>();
    }

    #endregion Udp相关

    #region 商城相关

    /// <summary>
    /// 
    /// </summary>
    public class ProductInfoItem
    {
        /// <summary>
        /// 平台商品Id。如:com.duangsea.01
        /// </summary>
        [JsonPropertyName("productStoreId")]
        public string ProductStoreId { get; set; }

        /// <summary>
        /// 商品单价。
        /// </summary>
        [JsonPropertyName("productAmount")]
        public decimal ProductAmount { get; set; }

        /// <summary>
        /// 币种缩写。如 USD。
        /// </summary>
        [JsonPropertyName("currencyCode")]
        public string CurrencyCode { get; set; }

        /// <summary>
        /// 币种符号。如 $。
        /// </summary>
        [JsonPropertyName("currencyMark")]
        public string CurrencyMark { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class GameShoppingItem : IValidatableObject, ICloneable
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
        public List<GameEntitySummary> Outs { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            return new GameShoppingItem
            {
                Period = Period.Clone() as GamePeriod,
                GroupNumber = GroupNumber,
                Ins = new List<BlueprintInItem>(Ins.Select(c => c.Clone() as BlueprintInItem)),
                MaxCount = MaxCount,
                Outs = Outs.Select(c => c.Clone() as GameEntitySummary).ToList(),
            };
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }
    }

    /// <summary>
    /// 定义周期的类。
    /// </summary>
    public class GamePeriod : IValidatableObject, ICloneable
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

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Start > End)
                yield return new ValidationResult($"{nameof(Start)}必须不晚于{nameof(End)}");
        }

#if NETCOREAPP3_0_OR_GREATER

        TimeSpanEx? _Period;
        /// <summary>
        /// 循环周期长度。
        /// </summary>
        [JsonIgnore]
        public TimeSpanEx Period => _Period.HasValue ? _Period.Value : (_Period = new TimeSpanEx(PeriodString)).Value;

        TimeSpanEx? _ValidPeriod;
        /// <summary>
        /// 有效周期长度。
        /// </summary>
        [JsonIgnore]
        public TimeSpanEx ValidPeriod => _ValidPeriod.HasValue ? _ValidPeriod.Value : (_ValidPeriod = new TimeSpanEx(ValidPeriodString)).Value;

        /// <summary>
        /// 获取指定时间点是否在该对象标识的有效期内。
        /// </summary>
        /// <param name="now"></param>
        /// <param name="start">返回true时这里返回<paramref name="now"/>时间点所处周期的起始时间点。其它情况此值是随机值。</param>
        /// <returns></returns>
        public bool IsValid(DateTime now, out DateTime start)
        {
            start = Start;
            if (now < start || now > End)   //若已经超期
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_INVALID_DATA,
                    $"指定的时间 {now} 不在指定的时间范围内{Start} - {End}。");
                return false;
            }
            for (start = Start; now >= start; start += Period)
            {
                if (now < start + ValidPeriod)    //若找到合适的项
                {
                    OwHelper.SetLastError(ErrorCodes.NO_ERROR);
                    return true;
                }
            }
            OwHelper.SetLastError(ErrorCodes.ERROR_INVALID_DATA);
            OwHelper.SetLastErrorMessage($"指定的时间{now}不在商品有效期内。");
            return false;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            return new GamePeriod
            {
                End = End,
                PeriodString = PeriodString,
                Start = Start,
                ValidPeriodString = ValidPeriodString,
            };
        }

#endif
    }

    #endregion 商城相关

    #region 成就相关

    /// <summary>
    /// 成就/任务定义。
    /// </summary>
    public class GameAchievementTO : IValidatableObject
    {
        /// <summary>
        /// 
        /// </summary>
        public GameAchievementTO()
        {

        }

        /// <summary>
        /// 该项的游戏周期。仅在有效周期内才会计算任务/成就。
        /// </summary>
        public GamePeriod Period { get; set; } = new GamePeriod();

        /// <summary>
        /// 需求/消耗物的集合。可以配置该任务/成就的前置任务/成就。
        /// </summary>
        public BlueprintInItem[] Ins { get; set; }

        /// <summary>
        /// 经验到等级转换用的序列，如[100,200]表示指标值>=100时达成该成就第1级（未达成前是0级），当指标值>=200时达成第2级成就；以此类推。
        /// </summary>
        public decimal[] Exp2LvSequence { get; set; }

        /// <summary>
        /// 图鉴成就特有的条件。
        /// </summary>
        public BlueprintInItem[] TjIns { get; set; }

        /// <summary>
        /// 产出物的集合。对应每个级别产出，每个级别可以产出多个物品。
        /// </summary>
        public List<GameEntitySummary[]> Outs { get; set; } = new List<GameEntitySummary[]>();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!(Exp2LvSequence?.Length > 0))
                yield return new ValidationResult($"成就的{nameof(Exp2LvSequence)}属性至少要有一个元素。");
        }
    }

    #endregion

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
                Debug.WriteLine($"行Id={Id}——{PropertiesString}{Environment.NewLine}{err.Message}");
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
    public partial class TemplateStringFullView : IValidatableObject
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
        public string[] Genus { get; set; } = Array.Empty<string>();

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

        /// <summary>
        /// 支付平台的商品Id。
        /// </summary>
        [JsonPropertyName("productStoreId")]
        public string ProductStoreId { get; set; }

        /// <summary>
        /// 商品平台价格。
        /// </summary>
        [JsonPropertyName("productAmount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 商品平台币种。
        /// </summary>
        [JsonPropertyName("currencyCode")]
        public string CurrencyCode { get; set; }

        /*//内网-开发调试
            //DevelopPrivy_Android
            //DevelopPrivy_iOS

            //内网-内部开发
            //DevelopChina_Android
            //DevelopChina_iOS

            //冰鸟-韩国-线上测试
            //Korea_Android
            //Korea_iOS

            //北美-加拿大-线上测试
            //Canada_Android
            //Canada_iOS

            //香港-港澳台-线上测试
            //Hongkong_Android
            //Hongkong_iOS

            //飞书-南美-线上测试
            //Feishu_Android
            //Feishu_iOS

            //自测-菲律宾-线上测试
            //Pilipinas_Android
            //Pilipinas_iOS

            //版号-国内Tap-线上测试
            //TapTap_Android
            //TapTap_iOS

            //发行-东南亚-线上测试
            //Southeast_Android
            //Southeast_iOS

            //支付后台商品id属性名：productStoreId
        //productAmount
        //currencyCode
        //currencyMark
            //商品信息-区分渠道平台, Dictionary<渠道平台,Dictionary<属性名,值>>
        */
        /// <summary>
        /// 渠道信息。
        /// </summary>
        [JsonPropertyName("productInfo")]
        public Dictionary<string, Dictionary<string, string>> ProductInfo { get; set; } = new Dictionary<string, Dictionary<string, string>>();
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
        public List<GameEntitySummary> Out { get; set; } = new List<GameEntitySummary>();

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
        /// 关卡入场条件。
        /// </summary>
        public List<BlueprintInItem> Ins { get; set; } = new List<BlueprintInItem>();

        /// <summary>
        /// 通关等级转化数组。可能是空集合表示不评定星级。
        /// </summary>
        /// <value>例如:[ 360, 480, 600 ]小于或等于360秒则评定为3(数组长度)星，大于600秒则评定为0星，以此类推。</value>
        [JsonPropertyName("scoreTime")]
        public List<decimal> ScoreTime { get; set; } = new List<decimal>();
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

        /// <summary>
        /// 卡池组数据。
        /// </summary>
        public GameDiceGoup DiceGroup { get; set; }

        #endregion 骰子相关

        #region 动态产出相关

        /// <summary>
        /// 动态产出对象。
        /// </summary>
        public SequenceOut SequenceOut { get; set; }

        #endregion 动态产出相关

        #region 成就相关
        /// <summary>
        /// 成就数据。
        /// </summary>
        public GameAchievementTO Achievement { get; set; }
        #endregion

        #region 唯一性相关

        /// <summary>
        /// 当该模板所代表物品需要在角色范围内做唯一性验证时配置此数据。一旦配置了该属性不是null则在发现违反角色唯一性的时候使用<see cref="GameEntitySummary"/>进行转化。
        /// </summary>
        public List<GameEntitySummary> UniInCharOuts { get; set; } = null;

        #endregion 唯一性相关

        #region 事件相关

        /// <summary>
        /// 游戏内事件模板。
        /// </summary>
        public GameEventTO GameEvent { get; set; }

        #endregion 事件相关

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

        /// <summary>
        /// Count 到等级转换用的序列，如[100,200]表示指标值>=100时达成该成就第1级（未达成前是0级），当指标值>=200时达成第2级成就；以此类推。
        /// 此对象为null或空数组则 不会转换级别。
        /// </summary>
        public decimal[] Exp2LvSequence { get; set; }


        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        public GameMailTo Mail { get; set; }
    }

    /// <summary>
    /// 孵化的信息项主要数据封装类。
    /// </summary>
    public class FuhuaInfo : IValidatableObject
    {
        /// <summary>
        /// 双亲1的条件。
        /// </summary>
        public GameThingPreconditionItem[] Parent1Conditional { get; set; }

        /// <summary>
        /// 双亲2的条件。
        /// </summary>
        public GameThingPreconditionItem[] Parent2Conditional { get; set; }

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

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return default;
        }
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
        public GameThingPreconditionItem[] Conditional { get; set; }

        /// <summary>
        /// 消耗的数量。第一个值是由0级升级到1级这个动作的消耗数量。
        /// 注意消耗数量可能是0，代表需要此物品但不消耗此物品。若是null或空则表示所有级别都不消耗。
        /// </summary>
        public List<decimal> Counts { get; set; } = new List<decimal>();
    }

    /// <summary>
    /// 针对数值属性的组合条件，可以用于限定角色自己的某些周期性行为。
    /// 以下条件为真：(获取属性值 - Subtrahend) 对 Modulus 求余数，余数要在 [MinRemainder, MaxRemainder] 邻域中。
    /// </summary>
    public class NumberCondition : IValidatableObject, ICloneable
    {
        /// <summary>
        /// 属性名，通常是Count。该属性必须是一个数值型的属性。
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// 最小值。省略或为null表示不限制。
        /// </summary>
        [AllowNull]
        public decimal? MinValue { get; set; }

        /// <summary>
        /// 最大值。省略或为null表示不限制。
        /// </summary>
        public decimal? MaxValue { get; set; }

        /// <summary>
        /// 减数。省略视同为0。
        /// </summary>
        public decimal Subtrahend { get; set; }

        /// <summary>
        /// 求模的模数。
        /// </summary>
        public decimal Modulus { get; set; }

        /// <summary>
        /// 最小余数。
        /// </summary>
        public decimal MinRemainder { get; set; }

        /// <summary>
        /// 最小余数。
        /// </summary>
        public decimal MaxRemainder { get; set; }

        /// <summary>
        /// 返回一个深表副本。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            return new NumberCondition
            {
                MaxRemainder = MaxRemainder,
                MaxValue = MaxValue,
                Subtrahend = Subtrahend,
                MinRemainder = MinRemainder,
                MinValue = MinValue,
                Modulus = Modulus,
                PropertyName = PropertyName,
            };
        }

        /// <summary>
        /// 从对象上获取属性值。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>null表示对象上没有指定名称的属性或属性值无法转换为数值。否则返回属性值。</returns>
        public decimal? GetNumber(object obj)
        {
            if (obj.GetType().GetProperty(PropertyName) is PropertyInfo pi && OwConvert.TryToDecimal(pi.GetValue(obj), out var deci)) return deci; //若非数值属性
            return null;
        }

        /// <summary>
        /// 获取指示：指定属性数是否在有效范围内。
        /// </summary>
        /// <param name="now"></param>
        /// <returns>true有效，false无效。</returns>
        public bool IsMatch(decimal now)
        {
            if (now < MinValue || now > MaxValue) return false;   //若本身超出范围
            var ren = (now - Subtrahend) % Modulus; //求余数
            return ren >= MinRemainder && ren <= MaxRemainder;
        }

        /// <summary>
        /// 获取属性数，处于第几个周期内。
        /// </summary>
        /// <param name="now"></param>
        /// <returns>null当前不在有效周期内，否则返回所处周期数，从0开始。</returns>
        public int? GetPeriodIndex(decimal now)
        {
            if (!IsMatch(now)) return null;
            var floor = Convert.ToInt32(Math.Floor((now - Subtrahend) / Modulus));  //整除商 ,周期号
            return floor;
        }

        /// <summary>
        /// 获取指定数值所处周期。仅能对整数求解。
        /// </summary>
        /// <param name="now">要测试的属性数。</param>
        /// <param name="start">所处周期的起始数。</param>
        /// <param name="end">所处周期的结束数。</param>
        /// <returns>true指定值在有效周期内，false指定值不在指定周期内。</returns>
        public bool GetCurrentPeriod(decimal now, out decimal start, out decimal end)
        {
            var floor = GetPeriodIndex(now);  //周期号
            if (floor is null) goto lbErr;
            start = floor.Value * Modulus + MinRemainder + Subtrahend;
            end = start - MinRemainder + MaxRemainder;
            return true;
        lbErr:
            start = end = 0;
            return false;
        }

        /// <summary>
        /// 获取第一个周期的起始和终止数。
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>true找到了第一个有效周期，false没有找到有效周期。</returns>
        public bool GetFirstPeriod(out decimal start, out decimal end)
        {
            var tmp = MinValue ?? 0;
            for (int i = 0; i < Modulus; i++)
            {
                if (GetCurrentPeriod(i, out start, out end)) return true;
            }
            start = end = 0;
            return false;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }
    }

    /// <summary>
    /// 定位一个物品的条件的详细项。如果指定多种属性过滤则需要满足所有属性要求。
    /// </summary>
    public partial class GameThingPreconditionItem : IValidatableObject, ICloneable
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameThingPreconditionItem()
        {
        }

        #region 简写属性，都可以使用通用属性替代，仅仅因为常用所以提供了简写的方式

        /// <summary>
        /// 物品的TId。省略则不限制。
        /// </summary>
        public Guid? TId { get; set; }

        /// <summary>
        /// 容器的模板Id。省略则不限制。
        /// </summary>
        public Guid? ParentTId { get; set; }

        /// <summary>
        /// 需要包含的属名称（如果有多项则必须全部包含）。空集合则不限制。
        /// </summary>
        public List<string> Genus { get; set; } = new List<string>();

        /// <summary>
        /// 针对数值属性的组合条件，可以用于限定角色自己的某些周期性行为。省略或为null表示不限定。
        /// </summary>
        public NumberCondition NumberCondition { get; set; }

        /// <summary>
        /// 要求的最小数量。省略(null)则不限制。
        /// </summary>
        public decimal? MinCount { get; set; }

        /// <summary>
        /// 要求的最大数量。省略(null)则不限制。
        /// </summary>
        public decimal? MaxCount { get; set; }

        #endregion 简写属性

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
        /// 条件组掩码。最多可以有32个条件组。在测试是否满足条件时，根据条件组选取的掩码来测试条件。如要求符合条件的掩码是2，则此属性为2，3，6，7的条件都被考虑在内。
        /// 目前预先定义的值：1 表示执行条件组，2 表示获取列表的条件组（未来可能有其它定义，如4可能表示预览条件组，它既不同于执行也不同于获取列表的要求）。
        /// 设置为3则表示执行和获取列表时都要考虑在内。
        /// </summary>
        /// <value>值为0则表示这是一个旧式条件，不使用掩码决定测试的场景。仅为兼容性考虑，暂时保留0.未来0是临时使该条件失效的设置。</value>
        public int GroupMask { get; set; }
            = 3;    //暂时预制为3，需求使然

        /// <summary>
        /// 此项是否符合掩码条件。
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public bool IsValidate(int mask) =>
            (GroupMask & mask) == mask;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            return new GameThingPreconditionItem
            {
                GeneralConditional = new List<GeneralConditionalItem>(GeneralConditional.Select(c => c.Clone() as GeneralConditionalItem)),
                Genus = new List<string>(Genus),
                NumberCondition = NumberCondition.Clone() as NumberCondition,
                GroupMask = GroupMask,
                MinCount = MinCount,
                MaxCount = MaxCount,
                ParentTId = ParentTId,
                TId = TId,
            };
        }
    }

    /// <summary>
    /// 一个通用的表达式对象。计划用于从寻找到的实体上提取属性。
    /// 当前版本可能是一个bool或数值。
    /// </summary>
    public class GeneralConditionalItem : IValidatableObject, ICloneable
    {
        /// <summary>
        /// 有效操作符列表。
        /// </summary>
        public static string[] ValidOperator { get; } = new string[] { ">=", ">", "<=", "<", "==", "!=", "ModE" };

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GeneralConditionalItem()
        {

        }

        #region 公共属性

        /// <summary>
        /// 操作符（函数名）。详细说明参见remarks。
        /// </summary>
        /// <remarks>
        /// ModE 返回bool类型， 如{"op":"ModE","PropertyName":"Count","args":["7","1"]}表示实体的Count属性对7求余数等于1则符合条件，否则不符合。
        /// ToInt32 返回decimal类型，有唯一的参数，字符串类型，标识获取实体属性的名称。如{"op":"ModE","args":["Count"]}表示返回指定实体的Count属性值。
        /// </remarks>
        [JsonPropertyName("op")]
        public string Operator
        {
            get; set;
        }

        /// <summary>
        /// 属性名。该属性只能是一个可以运算的类型，即可以转化为<see cref="decimal"/>的类型。
        /// 可能省略，对于新版本这个属性保留不用，未来可能删除。
        /// </summary>
        [JsonPropertyName("pn")]
        public string PropertyName
        {
            get; set;
        }

        List<string> _Args;
        /// <summary>
        /// 一组参数值,参数的顺序和数量由运算符指定。，有多少个元素由运算符指定。
        /// </summary>
        [JsonPropertyName("args")]
        public List<string> Args { get => _Args ?? (_Args = new List<string> { }); set => _Args = value; }

        #endregion 公共属性

        #region 辅助方法

        bool VerifyCount<T>(ICollection<T> objs, int count)
        {
            if (objs.Count != count)
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_BAD_ARGUMENTS, $"{Operator} 需要 {count} 个参数，但是实际有 {objs.Count} 个参数");
                return false;
            }
            return true;
        }
        #endregion 辅助方法

        #region 公共方法 为客户端保留，服务器不再使用

        /// <summary>
        /// 获取指定属性的反射对象。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="propertyInfo"></param>
        /// <returns>true找到值，false出错。</returns>
        public bool TryGetPropertyInfo(object entity, out PropertyInfo propertyInfo)
        {
            propertyInfo = entity.GetType().GetProperty(PropertyName);
            if (propertyInfo is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"无法找到指定的属性{PropertyName}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 获取指定属性的值。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="result"></param>
        /// <returns>true找到值，false出错。</returns>
        public bool TryGetPropertyValue(object entity, out object result)
        {
            if (!TryGetPropertyInfo(entity, out var pi))
            {
                result = default;
                return false;
            }
            result = pi.GetValue(entity);
            return true;
        }

        /// <summary>
        /// 获取条件对象中指定的属性值。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="result"></param>
        /// <returns>true找到值，false出错。</returns>
        public bool TryGetDecimal(object entity, out decimal result)
        {
            if (!TryGetPropertyValue(entity, out var obj))
            {
                result = default;
                return false;
            }
            try
            {
                result = Convert.ToDecimal(obj);
            }
            catch (Exception)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"属性值无法转化为Decimal,{obj}");
                goto lbErr;
            }
            return true;
        lbErr:
            result = default;
            return false;
        }

        /// <summary>
        /// 获取一个指示指定实体是否符合指定条件。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="ignore">保留未用。</param>
        /// <returns>true符合条件，false不符合条件或出错，<see cref="OwHelper.GetLastError()"/>是ErrorCodes.NO_ERROR则是不符合条件。</returns>
        public bool IsMatch(object entity, bool ignore = false)
        {
            bool result;
            decimal val;    //属性的值
            switch (Operator)
            {
                case "ModE":
                    //获取属性值
                    if (!TryGetDecimal(entity, out val))
                    {
                        result = false;
                        break;
                    }
                    if (Args.Count < 2)
                    {
                        OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                        OwHelper.SetLastErrorMessage($"ModE要有两个参数但实际只有{Args.Count}个参数。");
                        result = false;
                        break;
                    }
                    else if (Args.Count > 2)
                        OwHelper.SetLastErrorMessage($"ModE要有两个参数但实际有{Args.Count}个参数。程序将忽略尾部多余参数");
                    if (!OwConvert.TryToDecimal(Args[0], out var arg0) || !OwConvert.TryToDecimal(Args[1], out var arg1))
                    {
                        OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                        OwHelper.SetLastErrorMessage($"ModE要有两个参数都是数值型。");
                        result = false;
                        break;
                    }
                    else
                        OwHelper.SetLastError(ErrorCodes.NO_ERROR);
                    result = val % arg0 == arg1;
                    break;
                case "<=":
                //获取属性值
                //if (!TryGetDecimal(entity, out val))
                //{
                //    result = false;
                //    break;
                //}
                //if (Args.Count < 1)
                //{
                //    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                //    OwHelper.SetLastErrorMessage($"<=要有一个参数但实际只有{Args.Count}个参数。");
                //    result = false;
                //    break;
                //}
                //else if (Args.Count > 1)
                //    OwHelper.SetLastErrorMessage($"<=要有一个参数但实际有{Args.Count}个参数。程序将忽略尾部多余参数");
                //else
                //    OwHelper.SetLastError(ErrorCodes.NO_ERROR);
                //result = val <= Args[0];
                //break;
                case "<":
                case ">=":
                case ">":
                case "==":
                case "!=":
                    throw new NotImplementedException();
                default:
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    OwHelper.SetLastErrorMessage($"不支持的操作符{Operator}");
                    result = false;
                    break;
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">返回值</param>
        /// <param name="objs">运行时附属参数。</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool TryGetValue(out object value, params object[] objs)
        {
            var result = false;
            switch (Operator)
            {
                case "ToInt32":
                    if (!VerifyCount(Args, 1) || !VerifyCount(objs, 1))
                    {
                        value = default;
                        break;
                    }
                    var obj = objs[0];
                    if (!TryGetPropertyValue(obj, out var tmp) || !OwConvert.TryToDecimal(tmp, out var deci))
                    {
                        value = default;
                        break;
                    }
                    value = Convert.ToInt32(deci);  //舍入为最接近的 32 位有符号整数。 如果 value 为两个整数中间的数字，则返回二者中的偶数；即 4.5 转换为 4，而 5.5 转换为 6。
                    break;
                default:
                    value = default;
                    break;
            }
            return result;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            return new GeneralConditionalItem
            {
                Args = new List<string>(Args),
                Operator = Operator,
                PropertyName = PropertyName,
            };
        }

        #endregion 公共方法

    }

    #region 合成相关

    /// <summary>
    /// 合成的材料信息。
    /// </summary>
    public class BlueprintInItem : IValidatableObject, ICloneable
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
        public List<GameThingPreconditionItem> Conditional { get; set; } = new List<GameThingPreconditionItem>();

        /// <summary>
        /// 消耗的数量。
        /// 注意消耗数量可能是0，代表需要此物品但不消耗此物品。
        /// </summary>
        public decimal Count { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            return new BlueprintInItem
            {
                Conditional = Conditional.Select(c => c.Clone() as GameThingPreconditionItem).ToList(),
                Count = Count,
            };
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }
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
        public List<GameEntitySummary> Out { get; set; } = new List<GameEntitySummary>();

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
    /// 骰子池的组。组内所有池子共享抽取优惠次数。
    /// </summary>
    public class GameDiceGoup
    {
        /// <summary>
        /// 骰子池的TId集合。
        /// </summary>
        public List<Guid> DiceIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 达到优惠次数时使用特定的抽取规则。如80，表示地80次抽取时使用<see cref="GuaranteesDiceTId"/>指定的卡池抽取。可以是null。
        /// </summary>
        public int GuaranteesCount { get; set; }

        /// <summary>
        /// 达到优惠次数时，使用此TId使用的卡池进行1次抽奖。可以是null。
        /// </summary>
        public Guid GuaranteesDiceTId { get; set; }
    }

    /// <summary>
    /// 定义一个"池子"
    /// </summary>
    public class GameDice : IValidatableObject, ICloneable
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

        /// <summary>
        /// 达到优惠次数时使用特定的抽取规则。如80，表示地80次抽取时使用<see cref="GuaranteesDiceTId"/>指定的卡池抽取。可以是null。
        /// </summary>
        public int? GuaranteesCount { get; set; }

        /// <summary>
        /// 达到优惠次数时，使用此TId使用的卡池进行1次抽奖。可以是null。
        /// </summary>
        public Guid? GuaranteesDiceTId { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }

        /// <summary>
        /// 获取一个深表副本。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            var result = new GameDice
            {
                AllowRepetition = AllowRepetition,
                GuaranteesCount = GuaranteesCount,
                GuaranteesDiceTId = GuaranteesDiceTId,
                MaxCount = MaxCount,
            };
            result.Items.AddRange(Items.Select(c => (GameDiceItem)c.Clone()));
            return result;
        }
    }

    /// <summary>
    /// 池子项。
    /// </summary>
    public class GameDiceItem : IValidatableObject, ICloneable
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameDiceItem()
        {
        }

        /// <summary>
        /// 产出物品的描述集合。
        /// </summary>
        public List<GameEntitySummary> Outs { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// 权重值，可以带小数。在同一个池子中所有项加起来的权重是分母，该项权重是分子。
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// 保底忽略标志。
        /// </summary>
        /// <value>true当命中此项时会清除保底计数，置为0。</value>
#if NET5_0_OR_GREATER
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#endif
        public bool ClearGuaranteesCount { get; set; }

        /// <summary>
        /// 获取一个深表副本。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            var result = new GameDiceItem
            {
                ClearGuaranteesCount = ClearGuaranteesCount,
                Weight = Weight,
            };
            result.Outs.AddRange(Outs.Select(c => (GameEntitySummary)c.Clone()));
            return result;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
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
        /// <value>默认值：<see cref="OwHelper.WorldNow"/></value>
        public DateTime LastDateTime { get; set; } = OwHelper.WorldNow;

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
            DateTime now = OwHelper.WorldNow;
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
        /// <param name="currentValue">这个时间点不晚于指定时间点，且又是正好一跳的时间点。</param>
        /// <param name="dateTime"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLastValue(decimal currentValue, ref DateTime dateTime)
        {
            var remainder = (dateTime - LastDateTime).Ticks % Delay.Ticks;
            LastDateTime = dateTime.AddTicks(-remainder);
            if (LastDateTime > dateTime)    //若时间点超过指定值
                LastDateTime -= Delay;
            dateTime = LastDateTime;
            _CurrentValue = currentValue;
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

    #region 邮件相关
    /// <summary>
    /// 邮件模板。
    /// </summary>
    public class GameMailTo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameMailTo()
        {

        }

        #region 基本属性

        /// <summary>
        /// 邮件标题。
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// 邮件正文。
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// 附件集合。
        /// </summary>
        public List<GameEntitySummary> Attachment { get; set; } = new List<GameEntitySummary> { };

        /// <summary>
        /// 存储一些特殊属性的字典。
        /// </summary>
        public Dictionary<string, string> Dictionary1 { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 存储一些特殊属性的字典。
        /// </summary>
        public Dictionary<string, string> Dictionary2 { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 对没有附件且已读的邮件，多长时间删除。若为空则等待最长删除时间到来，当前是60天。
        /// </summary>
        public TimeSpan? DeleteDelay { get; set; }

        #endregion 基本属性

    }

    #endregion 邮件相关

    #region 法币购买相关

    #endregion 法币购买相关

    #region 事件相关

    /// <summary>
    /// 事件模板类。
    /// </summary>
    public class GameEventTO
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameEventTO()
        {

        }

        /// <summary>
        /// 事件Id。这个Id有由服务器给列表，标记出每个id的意义。
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// 守卫条件，仅当所有条件满足时猜执行后续任务。
        /// </summary>
        public BlueprintInItem[] Ins { get; set; }

        /// <summary>
        /// 要改变的实体摘要集合。特别地，其中 GameEntitySummary.Count 属性应设置为0，表示使用事件的增量值（如增加了多少个击杀的精英怪）替代。
        /// </summary>
        public GameEntitySummary[] Outs { get; set; }
    }
    #endregion 事件相关

}
