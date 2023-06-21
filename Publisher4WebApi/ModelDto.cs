﻿using AutoMapper;
using AutoMapper.Configuration.Annotations;
using GY02.Commands;
using GY02.Templates;
using OW.Game.Entity;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace GY02.Publisher
{
    #region 基础数据结构

    /// <summary>
    /// 游戏内装备/道具的摘要信息。
    /// </summary>
    [AutoMap(typeof(GameEntitySummary), ReverseMap = true)]
    public class GameEntitySummaryDto
    {
        /// <summary>
        /// 特定原因记录物品唯一Id，通常为null。
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// 父容器模板Id，为null则放置在默认容器中。
        /// </summary>
        public Guid? ParentTId { get; set; }

        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 数量。
        /// </summary>
        public decimal Count { get; set; }
    }

    /// <summary>
    /// 返回对象的基类。
    /// </summary>
    public class ReturnDtoBase
    {
        /// <summary>
        /// 
        /// </summary>
        public ReturnDtoBase()
        {

        }

        /// <summary>
        /// 是否有错误。不设置则使用<see cref="ErrorCode"/>来判定。
        /// </summary>
        /// <value>0没有错误，其它数值含义由应用定义。</value>
        public bool HasError { get; set; }

        /// <summary>
        /// 错误码，参见 ErrorCodes。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage { get; set; }

    }

    /// <summary>
    /// 带有令牌命令的入参基类。
    /// </summary>
    public class TokenDtoBase
    {
        /// <summary>
        /// 令牌。
        /// </summary>
        public Guid Token { get; set; }
    }

    /// <summary>
    /// 模板基类传输类。
    /// </summary>
    public class TemplateDto
    {

    }

    /// <summary>
    /// 模板数据的传输类。
    /// </summary>
    public class Gy02TemplateDto : TemplateDto
    {

    }

    /// <summary>
    /// 
    /// </summary>
    public class VirtualThingDto
    {
        /// <summary>
        /// 唯一Id。
        /// </summary>
        public Guid Id { get; set; }

        private byte[] _BinaryArray;
        /// <summary>
        /// 扩展的二进制大对象。
        /// </summary>
        public byte[] BinaryArray
        {
            get { return _BinaryArray; }
            set { _BinaryArray = value; }

        }

        /// <summary>
        /// 所有扩展属性记录在这个字符串中，是一个Json对象。
        /// </summary>
        public string JsonObjectString { get; set; }

        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid ExtraGuid { get; set; }

        /// <summary>
        /// 记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象。
        /// </summary>
        public string ExtraString { get; set; }

        /// <summary>
        /// 记录一些额外的信息，用于排序搜索使用的字段。
        /// </summary>
        public decimal? ExtraDecimal { get; set; }

        /// <summary>
        /// 拥有的子物品或槽。
        /// </summary>
        public List<VirtualThingDto> Children { get; set; } = new List<VirtualThingDto>();

    }

    /// <summary>
    /// 游戏内玩家强类型数据的基类。
    /// </summary>
    [AutoMap(typeof(GameEntityBase))]
    public class GameJsonObjectBase
    {
        /// <summary>
        /// 唯一Id。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 等级。
        /// </summary>
        [JsonPropertyName("lv")]
        public decimal Level { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [AutoMap(typeof(GameEntity))]
    public class GameEntityDto : GameJsonObjectBase
    {
        /// <summary>
        /// 升级的累计消耗。
        /// </summary>
        public List<GameEntitySummaryDto> LvUpAccruedCost { get; set; } = new List<GameEntitySummaryDto>();

        /// <summary>
        /// 升品的累计用品。
        /// </summary>
        public List<GameEntitySummaryDto> CompositingAccruedCost { get; set; } = new List<GameEntitySummaryDto>();

        /// <summary>
        /// Count 属性最后的修改时间。
        /// </summary>
        public DateTime? CountOfLastModifyUtc { get; set; }
    }

    /// <summary>
    /// 账号数据。
    /// </summary>
    [AutoMap(typeof(GameUser))]
    public partial class GameUserDto
    {
        /// <summary>
        /// 登录名。
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// 当前使用角色。
        /// </summary>
        public GameCharDto CurrentChar { get; set; }

    }

    /// <summary>
    /// 购买记录的详细项。
    /// </summary>
    [AutoMap(typeof(GameShoppingHistoryItem))]
    public class GameShoppingHistoryItemDto
    {
        /// <summary>
        /// 默认构造函数。
        /// </summary>
        public GameShoppingHistoryItemDto()
        {

        }
        /// <summary>
        /// 购买的商品TId。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 购买的数量。
        /// </summary>
        public decimal Count { get; set; }

        /// <summary>
        /// 购买的日期。
        /// </summary>
        public DateTime DateTime { get; set; }
    }

    /// <summary>
    /// 投骰子的记录。
    /// </summary>
    [AutoMap(typeof(GameDiceHistoryItem))]
    public class GameDiceHistoryItemDto
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameDiceHistoryItemDto()
        {

        }

        /// <summary>
        /// 卡池或卡池组的TId。
        /// </summary>
        public Guid DiceTId { get; set; }

        /// <summary>
        /// 连续未命中高价值物品的次数。
        /// </summary>
        public int GuaranteesCount { get; set; }
    }

    /// <summary>
    /// 角色数据。
    /// </summary>
    [AutoMap(typeof(GameChar))]
    public partial class GameCharDto : GameEntityDto
    {
        #region 简单属性

        /// <summary>
        /// 昵称。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 攻击数值序列。
        /// </summary>
        [JsonPropertyName("atk")]
        public decimal Atk { get; set; }

        /// <summary>
        /// 防御数值序列。
        /// </summary>
        [JsonPropertyName("def")]
        public decimal Def { get; set; }

        /// <summary>
        /// 力量属性数值序列。
        /// </summary>
        [JsonPropertyName("pow")]
        public decimal Pow { get; set; }

        /// <summary>
        /// 暴击率。
        /// </summary>
        [JsonPropertyName("crit_pct")]
        public decimal CritPct { get; set; }

        /// <summary>
        /// 暴击倍数。1表示暴击和普通上海一致。
        /// </summary>
        [JsonPropertyName("crit")]
        public decimal Crit { get; set; }

        /// <summary>
        /// 角色当前所处战斗的关卡模板Id。
        /// 为null表示不在战斗中。
        /// </summary>
        public Guid? CombatTId { get; set; }

        /// <summary>
        /// 客户端用于记录战斗内信息的字符串。
        /// </summary>
        public string ClientCombatInfo { get; set; }

        /// <summary>
        /// 该角色登录的次数。
        /// </summary>
        public int LogineCount { get; set; }

        /// <summary>
        /// 最后一次登录的时间点。仅用户登录后才会改变该值，系统登录该角色不会改变该值。
        /// </summary>
        public DateTime? LastLoginDateTimeUtc { get; set; }

        #endregion 简单属性

        #region 各种槽

        /// <summary>
        /// 已穿戴皮肤槽。
        /// </summary>
        public GameSlotDto<GameEquipmentDto> YichuanPifuSlot { get; set; }

        /// <summary>
        /// 武器装备槽。
        /// </summary>
        public GameSlotDto<GameEquipmentDto> Wuqi { get; set; }

        /// <summary>
        /// 手套装备槽。
        /// </summary>
        public GameSlotDto<GameEquipmentDto> ShouTao { get; set; }

        /// <summary>
        /// 衣服装备槽。
        /// </summary>
        public GameSlotDto<GameEquipmentDto> YiFu { get; set; }

        /// <summary>
        /// 鞋子装备槽。
        /// </summary>
        public GameSlotDto<GameEquipmentDto> XieZi { get; set; }

        /// <summary>
        /// 腰带装备槽。
        /// </summary>
        public GameSlotDto<GameEquipmentDto> YaoDai { get; set; }

        /// <summary>
        /// 坐骑装备槽。
        /// </summary>
        public GameSlotDto<GameEquipmentDto> ZuoQiSlot { get; set; }

        /// <summary>
        /// 装备背包。
        /// </summary>
        public GameSlotDto<GameEquipmentDto> ZhuangBeiBag { get; set; }

        /// <summary>
        /// 道具背包。
        /// </summary>
        public GameSlotDto<GameItemDto> DaoJuBag { get; set; }

        /// <summary>
        /// 皮肤背包。
        /// </summary>
        public GameSlotDto<GameEquipmentDto> PiFuBag { get; set; }

        /// <summary>
        /// 货币槽。
        /// </summary>
        public GameSlotDto<GameItemDto> HuoBiSlot { get; set; }
        #endregion 各种槽

        #region 孵化相关

        /// <summary>
        /// 记录孵化的预览信息。
        /// </summary>
        public List<FuhuaSummaryDto> FuhuaPreview { get; set; } = new List<FuhuaSummaryDto>();

        /// <summary>
        /// 记录孵化系统正产出的历史数据。
        /// </summary>
        /// <remarks>是否可以生成皮肤以此处记录为准，没有则可以生成。</remarks>
        public List<FuhuaSummaryDto> FuhuaHistory { get; set; } = new List<FuhuaSummaryDto>();

        #endregion 孵化相关

        #region 战斗相关

        /// <summary>
        /// 通关信息相关集合。
        /// </summary>
        public List<CombatHistoryItemDto> CombatHistory { get; set; } = new List<CombatHistoryItemDto>();

        #endregion 战斗相关

        #region 商城相关

        /// <summary>
        /// 购买记录。
        /// </summary>
        public List<GameShoppingHistoryItemDto> ShoppingHistory { get; set; } = new List<GameShoppingHistoryItemDto>();

        #endregion 商城相关

        #region 投骰子的记录

        /// <summary>
        /// 投骰子的记录。
        /// </summary>
        public List<GameDiceHistoryItemDto> DiceHistory { get; set; } = new List<GameDiceHistoryItemDto>();
        #endregion 投骰子的记录

    }

    /// <summary>
    /// 道具数据。
    /// </summary>
    [AutoMap(typeof(GameItem))]
    public partial class GameItemDto : GameEntityDto
    {
        /// <summary>
        /// 数量。对非堆叠的是1。
        /// </summary>
        public decimal Count { get; set; }
    }

    /// <summary>
    /// 槽数据。
    /// </summary>
    /// <typeparam name="T">槽内数据类型。</typeparam>
    public partial class GameSlotDto<T> : GameJsonObjectBase
    {
        /// <summary>
        /// 
        /// </summary>
        public GameSlotDto()
        {
        }

        /// <summary>
        /// 槽的容量。
        /// </summary>
        public decimal Capacity { get; set; }

        /// <summary>
        /// 槽内的道具/装备。
        /// </summary>
        public ICollection<T> Children { get; set; } = new List<T>();
    }

    /// <summary>
    /// 装备数据。
    /// </summary>
    [AutoMap(typeof(GameEquipment))]
    public partial class GameEquipmentDto : GameEntityDto
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameEquipmentDto()
        {

        }

        #region 装备数据
        /// <summary>
        /// 攻击数值序列。
        /// </summary>
        [JsonPropertyName("atk")]
        public decimal Atk { get; set; }

        /// <summary>
        /// 防御数值序列。
        /// </summary>
        [JsonPropertyName("def")]
        public decimal Def { get; set; }

        /// <summary>
        /// 力量属性数值序列。
        /// </summary>
        [JsonPropertyName("pow")]
        public decimal Pow { get; set; }

        #endregion 装备数据
    }

    /// <summary>
    /// 带属性变化集合的返回值封装的基类。
    /// </summary>
    [Guid("A4EE8C3F-2FC7-4B45-A492-7BFB64ACDB57")]
    public class PropertyChangeReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 属性变化的集合。
        /// </summary>
        public List<GamePropertyChangeItemDto> Changes { get; set; } = new List<GamePropertyChangeItemDto>();
    }
    #endregion 基础数据结构

    #region 通用数据变化相关
    /// <summary>
    /// 用于精确描述变化数据的类。
    /// </summary>
    /// <remarks>这个类的新值和旧值都用Object表示，对于数据量极大的一些情况会使用具体的类表示如GamePropertyChangeFloatItemDto表示大量的即时战斗数据包导致的人物属性变化。</remarks>
    [Guid("905F93EE-D2A2-4321-87AD-C67CD145B77D")]
    public partial class GamePropertyChangeItemDto : IJsonData
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GamePropertyChangeItemDto()
        {
        }

        /// <summary>
        /// 对象的模板Id。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 对象Id。指出是什么对象变化了属性。
        /// </summary>
        public Guid ObjectId { get; set; }

        /// <summary>
        /// 属性的名字。事件发送者和处理者约定好即可，也可能是对象的其他属性名，如Children可以表示集合变化。
        /// </summary>
        public string PropertyName { get; set; }

        #region 旧值相关

        /// <summary>
        /// 指示<see cref="OldValue"/>中的值是否有意义。
        /// </summary>
        public bool HasOldValue { get; set; }

        /// <summary>
        /// 获取或设置旧值。
        /// </summary>
        public object OldValue { get; set; }

        #endregion 旧值相关

        #region 新值相关

        /// <summary>
        /// 指示<see cref="NewValue"/>中的值是否有意义。
        /// </summary>
        public bool HasNewValue { get; set; }

        /// <summary>
        /// 新值。
        /// </summary>
        public object NewValue { get; set; }

        #endregion 新值相关

        /// <summary>
        /// 属性发生变化的时间点。Utc计时。
        /// </summary>
        public DateTime DateTimeUtc { get; set; } = DateTime.UtcNow;
    }

    #endregion 通用数据变化相关

    #region 账号及登录相关

    /// <summary>
    /// 
    /// </summary>
    [AutoMap(typeof(LoginT78Command), ReverseMap = true)]
    public class LoginT78ParamsDto
    {
        /// <summary>
        /// 发行商SDK给的的sid。
        /// </summary>
        public string Sid { get; set; }

    }

    /// <summary>
    /// 
    /// </summary>
    [AutoMap(typeof(LoginT78Command))]
    public class LoginT78ReturnDto : LoginReturnDto
    {
        /// <summary>
        /// T78服务器返回的值完整的放在此处。仅当成功登录时才有。
        /// </summary>
        public string ResultString { get; set; }

        /// <summary>
        /// 登录名。
        /// </summary>
        [SourceMember("User.LoginName")]
        public string LoginName { get; set; }

        /// <summary>
        /// 密码。若首次登录，创建了账号则这里返回密码。否则返回null。
        /// </summary>
        public string Pwd { get; set; }
    }

    /// <summary>
    /// 心跳数据参数封装类。
    /// </summary>
    [AutoMap(typeof(NopCommand), ReverseMap = true)]
    public class NopParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 心跳功能返回数据封装类。
    /// </summary>
    [AutoMap(typeof(NopCommand))]
    public class NopReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 创建角色接口的参数封装类。
    /// </summary>
    [AutoMap(typeof(CreateAccountCommand), ReverseMap = true)]
    public class CreateAccountParamsDto
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CreateAccountParamsDto()
        {
        }

        #region 可映射属性

        /// <summary>
        /// 用户登录名。可省略则自动生成。
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// 密码。可省略则自动生成。
        /// </summary>
        public string Pwd { get; set; }

        #endregion 可映射属性
    }

    /// <summary>
    /// 创建接口返回数据封装类。
    /// </summary>
    [AutoMap(typeof(CreateAccountCommand))]
    public class CreateAccountResultDto : ReturnDtoBase
    {
        /// <summary>
        /// 
        /// </summary>
        public CreateAccountResultDto()
        {

        }

        #region 可映射属性

        /// <summary>
        /// 用户登录名。可省略，则自动指定。
        /// </summary>
        [AllowNull]
        public string LoginName { get; set; }

        /// <summary>
        /// 返回密码，客户端根据需要存储在本地，此后无法再明文返回密码。
        /// </summary>
        [AllowNull]
        public string Pwd { get; set; }

        #endregion 可映射属性
    }

    /// <summary>
    /// 
    /// </summary>
    [AutoMap(typeof(LoginCommand), ReverseMap = true)]
    public class LoginParamsDto
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public LoginParamsDto()
        {

        }

        #region 可映射属性

        /// <summary>
        /// 用户登录名。
        /// </summary>
        [Required]
        public string LoginName { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        [Required]
        public string Pwd { get; set; }

        #endregion 可映射属性
    }

    /// <summary>
    /// 登录接口返回数据封装类。
    /// </summary>
    [AutoMap(typeof(LoginCommand))]
    public class LoginReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public LoginReturnDto()
        {

        }

        #region 可映射属性

        /// <summary>
        /// 角色的信息。
        /// </summary>
        [SourceMember("User.CurrentChar")]
        public GameCharDto GameChar { get; set; }

        /// <summary>
        /// 用户账号的唯一Id。
        /// </summary>
        [SourceMember(nameof(LoginCommand.User) + "." + nameof(GameUser.Id))]
        public Guid UserId { get; set; }

        Guid _Token;
        /// <summary>
        /// 后续操作该用户使用的令牌。
        /// </summary>
        [SourceMember("User.Token")]
        public Guid Token
        {
            get => _Token; set
            {
                GyUdpClient.LastToken = value;
                _Token = value;
            }
        }

        /// <summary>
        /// 世界服务器的主机地址。使用此地址拼接后续的通讯地址。
        /// </summary>
        public string WorldServiceHost { get; set; }

        string _UdpServiceHost;
        /// <summary>
        /// Udp连接的地址。
        /// </summary>
        public string UdpServiceHost
        {
            get => _UdpServiceHost;
            set
            {
                GyUdpClient.LastUdpServiceHost = value;
                _UdpServiceHost = value;
            }
        }

        #endregion 可映射属性

    }
    #endregion 账号及登录相关

    #region 世界控制器功能相关

    /// <summary>
    /// 获取服务器时间接口返回值封装类。
    /// </summary>
    public class GetServerDateTimeUtcReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 服务器时间。
        /// </summary>
        public DateTime DateTimeUtc { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetTemplatesParamsDto
    {
        /// <summary>
        /// 用户名。
        /// </summary>
        public string Uid { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        public string Pwd { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetTemplates2ParamsDto
    {
        /// <summary>
        /// 用户名。
        /// </summary>
        [MaxLength(64)]
        public string Uid { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        [MaxLength(64)]
        public string Pwd { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetTemplates2ReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 模板数据集合。
        /// </summary>
        public IEnumerable<TemplateStringFullView> Templates { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class StopServiceParamsDto
    {
        /// <summary>
        /// 用户名。
        /// </summary>
        public string UId { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        public string Pwd { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class StopServiceReturnDto : ReturnDtoBase
    {
    }

    #endregion 世界控制器功能相关

    #region 物品管理相关

    /// <summary>
    /// 返回指定对象数据功能的参数封装类。
    /// </summary>
    [AutoMap(typeof(GetEntitiesCommand), ReverseMap = true)]
    public class GetEntitiesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 需要获取对象的唯一Id集合。
        /// 可以是角色Id,那样将返回角色对象。
        /// </summary>
        public List<Guid> Ids { get; set; } = new List<Guid>();

        /// <summary>
        /// 是否包含子对象。当前版本一律视同为false——都不包含子对象。
        /// </summary>
        public bool IncludeChildren { get; set; }
    }

    /// <summary>
    /// 返回指定对象数据功能的返回值封装类。
    /// </summary>
    [AutoMap(typeof(GetEntitiesCommand))]
    public class GetEntitiesReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回的实体集合。
        /// </summary>
        public List<GameItemDto> Results { get; set; } = new List<GameItemDto>();
    }

    /// <summary>
    /// 移动物品功能参数。
    /// </summary>
    [AutoMap(typeof(MoveItemsCommand), ReverseMap = true)]
    public class MoveItemsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要移动物品的Id集合。
        /// </summary>
        public List<Guid> ItemIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 要移动到的目标容器唯一Id。
        /// </summary>
        public Guid ContainerId { get; set; }
    }

    /// <summary>
    /// 移动物品功能返回数据。
    /// </summary>
    [AutoMap(typeof(MoveItemsCommand))]
    public class MoveItemsReturnDto : PropertyChangeReturnDto
    {

    }

    /// <summary>
    /// 增加物品参数封装类。
    /// </summary>
    public class AddItemsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要添加物品的模板Id。
        /// </summary>
        public List<Guid> TIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 增加的数量， 对应TIds中的顺序。
        /// </summary>
        public List<decimal> Counts { get; set; } = new List<decimal>();
    }

    /// <summary>
    /// 增加物品返回数据封装类。
    /// </summary>
    [AutoMap(typeof(MoveEntitiesCommand))]
    public class AddItemsReturnDto : PropertyChangeReturnDto
    {
    }

    #region 升降级相关

    /// <summary>
    /// 自动升级功能的参数封装类。
    /// </summary>
    [AutoMap(typeof(AutoLvUpCommand), ReverseMap = true)]
    public class AutoLvUpParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要自动升级的物品唯一Id。
        /// </summary>
        public Guid ItemId { get; set; }
    }

    /// <summary>
    /// 自动升级功能返回值封装类。
    /// </summary>
    [AutoMap(typeof(AutoLvUpCommand))]
    public class AutoLvUpReturnDto : PropertyChangeReturnDto
    {
    }

    /// <summary>
    /// 升级装备接口的参数封装类。
    /// </summary>
    [AutoMap(typeof(LvUpCommand), ReverseMap = true)]
    public class LvUpParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要升级物品的唯一Id集合。
        /// </summary>
        public List<Guid> Ids { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 升级装备接口的返回数据封装类。
    /// </summary>
    [AutoMap(typeof(LvUpCommand))]
    public class LvUpReturnDto : PropertyChangeReturnDto
    {
    }

    /// <summary>
    /// 降级接口返回参数封装类。
    /// </summary>
    [AutoMap(typeof(LvDownCommand), ReverseMap = true)]
    public class LvDownParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要降级的物品的唯一Id。
        /// </summary>
        public Guid ItemId { get; set; }
    }

    /// <summary>
    /// 降级接口返回数据封装类。
    /// </summary>
    [AutoMap(typeof(LvDownCommand))]
    public class LvDownReturnDto : PropertyChangeReturnDto
    {
    }

    #endregion 升降级相关

    /// <summary>
    /// 合成接口的参数封装类。
    /// </summary>
    [AutoMap(typeof(CompositeCommand), ReverseMap = true)]
    public class CompositeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 使用蓝图的模板Id。
        /// </summary>
        public Guid BlueprintId { get; set; }

        /// <summary>
        /// 主要材料的唯一Id。
        /// </summary>
        public Guid MainId { get; set; }

        /// <summary>
        /// 辅助材料的唯一Id的集合。降低品阶时这里不用填写。
        /// </summary>
        public List<Guid> Ids { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 合成接口的返回数据封装类。
    /// </summary>
    [AutoMap(typeof(CompositeCommand))]
    public class CompositeReturnDto : PropertyChangeReturnDto
    {
    }

    /// <summary>
    /// 自动合成紫色（不含）以下装备接口的返回数据封装类。
    /// </summary>
    [AutoMap(typeof(AutoCompositeCommand))]
    public class AutoCompositeReturnDto : PropertyChangeReturnDto
    {
    }

    /// <summary>
    /// 自动合成紫色（不含）以下装备功能的参数封装类。
    /// </summary>
    [AutoMap(typeof(AutoCompositeCommand), ReverseMap = true)]
    public class AutoCompositeParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 分解（降品）装备功能的参数封装类。
    /// </summary>
    [AutoMap(typeof(DecomposeCommand), ReverseMap = true)]
    public class DecomposeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要降品的装备的唯一Id。
        /// </summary>
        public Guid ItemId { get; set; }
    }

    /// <summary>
    /// 分解（降品）装备功能的返回值封装类。
    /// </summary>
    [AutoMap(typeof(DecomposeCommand))]
    public class DecomposeReturnDto : PropertyChangeReturnDto
    {
    }

    #endregion 物品管理相关

    #region 蓝图相关
    /// <summary>
    /// 
    /// </summary>
    public class ApplyBlueprintReturnDto : PropertyChangeReturnDto
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class ApplyBlueprintParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 使用的装备/道具等的唯一Id集合。
        /// </summary>
        public List<Guid> Items { get; set; }

        /// <summary>
        /// 使用的蓝图模板Id。
        /// </summary>
        public Guid BlueprintId { get; set; }
    }

    #endregion 蓝图相关

    #region 战斗相关

    /// <summary>
    /// 战斗的相关记录信息。
    /// </summary>
    [AutoMap(typeof(CombatHistoryItem), ReverseMap = true)]
    public class CombatHistoryItemDto
    {
        /// <summary>
        /// 关卡的模板Id。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 该关卡的最短时间，如果null,表示没有记录过。
        /// </summary>
        public TimeSpan? MinTimeSpanOfPass { get; set; }
    }


    /// <summary>
    /// 结算战斗功能参数封装类。
    /// </summary>
    [AutoMap(typeof(EndCombatCommand), ReverseMap = true)]
    public class EndCombatParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 战斗关卡的模板Id。
        /// </summary>
        public Guid CombatTId { get; set; }

        /// <summary>
        /// 掉落物品的集合。
        /// </summary>
        public List<GameEntitySummaryDto> Rewards { get; set; } = new List<GameEntitySummaryDto>();

        /// <summary>
        /// 杀怪或其它集合。
        /// </summary>
        public List<GameEntitySummaryDto> Others { get; set; } = new List<GameEntitySummaryDto>();

        /// <summary>
        /// 该关卡的最短时间，如果null,表示不记录。
        /// </summary>
        public TimeSpan? MinTimeSpanOfPass { get; set; }
    }

    /// <summary>
    /// 结算战斗返回值封装类。
    /// </summary>
    [AutoMap(typeof(EndCombatCommand))]
    public class EndCombatReturnDto : PropertyChangeReturnDto
    {

    }

    /// <summary>
    /// 记录战斗中信息功能的参数封装类。
    /// </summary>
    [AutoMap(typeof(CombatMarkCommand), ReverseMap = true)]
    public class CombatMarkParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要标记的战斗信息。
        /// </summary>
        public string CombatInfo { get; set; }
    }

    /// <summary>
    /// 记录战斗中信息返回信息封装类。
    /// </summary>
    [AutoMap(typeof(CombatMarkCommand))]
    public class CombatMarkReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 开始战斗功能参数封装类。
    /// </summary>
    [AutoMap(typeof(StartCombatCommand), ReverseMap = true)]
    public class StartCombatParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要启动的战斗模板Id。目前只有 905288ED-1615-4033-9F94-850C0C56E5C5 一个关卡。
        /// </summary>
        public Guid CombatTId { get; set; }
    }

    /// <summary>
    /// 开始战斗功能返回封装类。
    /// </summary>
    [AutoMap(typeof(StartCombatCommand))]
    public class StartCombatReturnDto : PropertyChangeReturnDto
    {
    }

    #endregion 战斗相关

    #region 孵化相关

    /// <summary>
    /// 孵化预览信息。
    /// </summary>
    [AutoMap(typeof(FuhuaSummary))]
    public class FuhuaSummaryDto
    {
        /// <summary>
        /// 双亲的类属集合，目前有两个元素，且按升序排序。
        /// </summary>
        public List<string> ParentTIds { get; set; } = new List<string>();

        /// <summary>
        /// 可能产出的物品预览。
        /// </summary>
        public List<GameDiceItemDto> Items { get; set; } = new List<GameDiceItemDto>();
    }

    /// <summary>
    /// 生成项的摘要信息。
    /// </summary>
    [AutoMap(typeof(GameDiceItem))]
    public class GameDiceItemDto
    {
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
        public bool ClearGuaranteesCount { get; set; }
    }

    /// <summary>
    /// 孵化预览功能所用的参数封装类。
    /// </summary>
    [AutoMap(typeof(FuhuaPreviewCommand), ReverseMap = true)]
    public class FuhuaPreviewParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 双亲的类属集合 如 "zuoqi_sheep" "zuoqi_wolf"，无所谓顺序，但返回时是按升序排序。
        /// </summary>
        public List<string> ParentGenus { get; set; } = new List<string>();

    }

    /// <summary>
    /// 孵化预览功能所用的返回值封装类。
    /// </summary>
    [AutoMap(typeof(FuhuaPreviewCommand))]
    public class FuhuaPreviewReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回数据，孵化可能生成的预览信息列表。
        /// </summary>
        public List<GameDiceItemDto> Result { get; set; } = new List<GameDiceItemDto>();
    }

    /// <summary>
    /// 孵化功能参数封装类。
    /// </summary>
    [AutoMap(typeof(FuhuaCommand), ReverseMap = true)]
    public class FuhuaParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 
        /// </summary>
        public FuhuaParamsDto()
        {

        }

        /// <summary>
        /// 双亲的类属集合 如 "zuoqi_sheep" "zuoqi_wolf"，无所谓顺序，但返回时是按升序排序。
        /// </summary>
        public List<string> ParentGenus { get; set; } = new List<string>();
    }

    /// <summary>
    /// 孵化功能返回值封装类。
    /// </summary>
    [AutoMap(typeof(FuhuaCommand))]
    public class FuhuaReturnDto : PropertyChangeReturnDto
    {
    }

    #endregion 孵化相关

    #region 商城相关

    /// <summary>
    /// 购买功能参数封装类。
    /// </summary>
    [AutoMap(typeof(ShoppingBuyCommand), ReverseMap = true)]
    public class ShoppingBuyParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 购买的商品项Id。
        /// </summary>
        public Guid ShoppingItemTId { get; set; }
    }

    /// <summary>
    /// 购买功能返回值封装类。
    /// </summary>
    [AutoMap(typeof(ShoppingBuyCommand))]
    public class ShoppingBuyReturnDto : PropertyChangeReturnDto
    {
    }

    /// <summary>
    /// 累计签到功能的参数封装类。
    /// </summary>
    [AutoMap(typeof(LeijiQiandaoCommand), ReverseMap = true)]
    public class LeijiQiandaoParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 累计签到功能的返回值封装类。
    /// </summary>
    [AutoMap(typeof(LeijiQiandaoCommand))]
    public class LeijiQiandaoReturnDto : PropertyChangeReturnDto
    {
    }

    /// <summary>
    /// 获取商城购买物品项功能的参数封装类。
    /// </summary>
    [AutoMap(typeof(GetShoppingItemsCommand), ReverseMap = true)]
    public class GetShoppingItemsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 页签过滤。如果这里有数据则仅返回这些页签下的商品项，若没有指定或为空集合，则返回所有页签的数据（这可能导致性能问题）
        /// </summary>
        public string[] Genus { get; set; }
    }

    /// <summary>
    /// 获取商城购买物品项功能的返回值封装类。
    /// </summary>
    [AutoMap(typeof(GetShoppingItemsCommand))]
    public class GetShoppingItemsReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 购买商品的状态集合。
        /// </summary>
        public List<ShoppingItemStateDto> ShoppingItemStates { get; set; } = new List<ShoppingItemStateDto>();
    }

    /// <summary>
    /// 购买商品的状态。
    /// </summary>
    [AutoMap(typeof(ShoppingItemState))]
    public class ShoppingItemStateDto
    {
        /// <summary>
        /// 默认构造函数。
        /// </summary>
        public ShoppingItemStateDto()
        {

        }

        /// <summary>
        /// 商品项的模板Id。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 当前周期下的已经购买数量。
        /// </summary>
        public decimal BuyedCount { get; set; }

        /// <summary>
        /// 记录该项属于的购买周期的起始时间点。
        /// </summary>
        public DateTime StartUtc { get; set; }

        /// <summary>
        /// 本周期的结束时间。空表示无结束时间。
        /// </summary>
        public DateTime? EndUtc { get; set; }
    }

    #endregion 商城相关

    #region 邮件相关

    /// <summary>
    /// 标记邮件为已读状态，且如果有附件则领取附件功能的参数封装类。
    /// </summary>
    [AutoMap(typeof(PickUpAttachmentCommand), ReverseMap = true)]
    public class MakeReadAndPickUpParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要标记已读且获取附件的邮件的唯一Id集合。
        /// </summary>
        public List<Guid> MailIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 标记邮件为已读状态，且如果有附件则领取附件功能的返回值封装类。
    /// </summary>
    [AutoMap(typeof(PickUpAttachmentCommand))]
    public class MakeReadAndPickUpReturnDto : PropertyChangeReturnDto
    {
    }

    /// <summary>
    /// 收取附件功能的参数封装类。
    /// </summary>
    [AutoMap(typeof(PickUpAttachmentCommand), ReverseMap = true)]
    public class PickUpAttachmentParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要获取附件的邮件的唯一Id集合。如果是空集合则获取所有邮件的附件。一个邮件的多个附件必须一次性全部获取。
        /// </summary>
        public List<Guid> MailIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 收取附件功能的返回值封装类。
    /// </summary>
    [AutoMap(typeof(PickUpAttachmentCommand))]
    public class PickUpAttachmentReturnDto : PropertyChangeReturnDto
    {
    }

    /// <summary>
    /// 收取邮件功能的参数封装类。
    /// </summary>
    [AutoMap(typeof(GetMailsCommand), ReverseMap = true)]
    public class GetMailsParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 收取邮件返回值封装类。
    /// </summary>
    [AutoMap(typeof(GetMailsCommand))]
    public class GetMailsReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 
        /// </summary>
        public GetMailsReturnDto()
        {

        }

        /// <summary>
        /// 返回收件箱中的邮件。空集合标识没有邮件。
        /// </summary>
        public List<GameMailDto> Mails { get; set; } = new List<GameMailDto>();
    }

    /// <summary>
    /// 发送邮件的内容。
    /// </summary>
    [AutoMap(typeof(SendMailItem), ReverseMap = true)]
    public class SendMailItemDto
    {
        /// <summary>
        /// 
        /// </summary>
        public SendMailItemDto()
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
        /// 存储一些特殊属性的字典。
        /// </summary>
        public Dictionary<string, string> Dictionary1 { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 存储一些特殊属性的字典。
        /// </summary>
        public Dictionary<string, string> Dictionary2 { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 附件集合。
        /// </summary>
        public List<GameEntitySummaryDto> Attachment { get; set; } = new List<GameEntitySummaryDto> { };

        /// <summary>
        /// 对没有附件且已读的邮件，多长时间删除。若为空则等待最长删除时间到来，当前是60天。
        /// </summary>
        public TimeSpan? DeleteDelay { get; set; }

        #endregion 基本属性

    }

    /// <summary>
    /// 
    /// </summary>
    [AutoMap(typeof(GameMail), ReverseMap = true)]
    public partial class GameMailDto
    {
        /// <summary>
        /// 
        /// </summary>
        public GameMailDto()
        {

        }

        #region 基本属性

        /// <summary>
        /// 该邮件的唯一Id标识。
        /// </summary>
        public Guid Id { get; set; }

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
        /// 发件人。可以不填写，自动用Token身份填入。
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// 收件人。不填写。
        /// </summary>
        public string To { get; set; }

        #endregion 基本属性

        #region 动态属性

        /// <summary>
        /// 已读的时间，null标识未读。
        /// </summary>
        public DateTime? ReadUtc { get; set; }

        /// <summary>
        /// 发件日期。
        /// </summary>
        public DateTime SendUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 领取附件的日期，null标识尚未领取。
        /// </summary>
        public DateTime? PickUpUtc { get; set; }

        /// <summary>
        /// 对没有附件且已读的邮件，多长时间删除。
        /// </summary>
        public TimeSpan? DeleteDelay { get; set; }

        /// <summary>
        /// 存储一些特殊属性的字典。
        /// </summary>
        public Dictionary<string, string> Dictionary1 { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 存储一些特殊属性的字典。
        /// </summary>
        public Dictionary<string, string> Dictionary2 { get; set; } = new Dictionary<string, string>();

        #endregion 动态属性

    }

    /// <summary>
    /// 发送邮件功能的参数封装类。
    /// </summary>
    [AutoMap(typeof(SendMailCommand), ReverseMap = true)]
    public class SendMailParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 
        /// </summary>
        public SendMailParamsDto()
        {

        }

        /// <summary>
        /// 邮件的本体。
        /// </summary>
        public SendMailItemDto Mail { get; set; }

        /// <summary>
        /// 发送的地址，对方角色唯一id的字符串，通常省略（空集合），表示群发,此时无法给自己发送邮件，要强制给自己发送，必须明确指定Id。
        /// </summary>
        public List<Guid> ToIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 发送邮件功能的返回值封装类。
    /// </summary>
    [AutoMap(typeof(SendMailCommand))]
    public class SendMailReturnDto : ReturnDtoBase
    {
    }

    #endregion 邮件相关

    #region 管理员功能相关

    /// <summary>
    /// 
    /// </summary>
    public class GetLiucunParamsDto
    {
        /// <summary>
        /// 注册的起始时间。
        /// </summary>
        public DateTime StartReg { get; set; }

        /// <summary>
        /// 注册的结束时间。
        /// </summary>
        public DateTime EndReg { get; set; }

        /// <summary>
        /// 登录起始时间。
        /// </summary>
        public DateTime StartLogin { get; set; }

        /// <summary>
        /// 登录的终止时间。
        /// </summary>
        public DateTime EndLogin { get; set; }

        /// <summary>
        /// 用户名。
        /// </summary>
        public string Uid { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        public string Pwd { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetLiucunReturnDto
    {
        /// <summary>
        /// 注册人数。
        /// </summary>
        public int RegCount { get; set; }

        /// <summary>
        /// 登陆人数。
        /// </summary>
        public int LoginCount { get; set; }

        /// <summary>
        /// 留存。
        /// </summary>
        public decimal Liucun { get; set; }
    }

    /// <summary>
    /// 用一组登录名获取当前角色Id的功能的参数封装类。
    /// </summary>
    public class GetCharIdByLoginNameParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 登录名集合。
        /// </summary>
        public List<string> LoginNames { get; set; } = new List<string>();
    }

    /// <summary>
    /// 用一组登录名获取当前角色Id的功能的返回值封装类。
    /// </summary>
    public class GetCharIdByLoginNameReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回的当前角色的Id的集合，与<see cref="LoginNames"/>顺序一致，若有错误的登录名则自动过滤掉。
        /// </summary>
        public List<Guid> CharIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 登录名集合。与<see cref="CharIds"/>顺序一致。
        /// </summary>
        public List<string> LoginNames { get; set; } = new List<string>();
    }

    #endregion 管理员功能相关
}


