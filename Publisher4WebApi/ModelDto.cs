using OW.Game.Store;
using System.ComponentModel.DataAnnotations;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using OW.Game.Entity;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using Gy02Bll.Templates;
using System.Text;
using AutoMapper;
using Gy02Bll.Commands;
using AutoMapper.Configuration.Annotations;
using System.Text.Json.Serialization;
using System.Net.NetworkInformation;
using OW.Game.PropertyChange;
using Gy02Bll.Commands.Account;
using Gy02Bll.Commands.Combat;
using Gy02Bll.Commands.Item;
using Gy02Bll.Commands.Fuhua;

namespace Gy02.Publisher
{
    #region 基础数据结构

    /// <summary>
    /// 游戏内装备/道具的摘要信息。
    /// </summary>
    [AutoMap(typeof(GameEntitySummary), ReverseMap = true)]
    public class GameEntitySummaryDto
    {
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
    [AutoMap(typeof(OwGameEntityBase))]
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
    public partial class GamePropertyChangeItemDto
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
    }
    #endregion Udp相关

    #region 账号及登录相关

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
            get => _UdpServiceHost; set
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
        public string Uid { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
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
    #endregion 世界控制器功能相关

    #region 物品管理相关

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
        public List<GameDiceItemSummaryDto> Items { get; set; } = new List<GameDiceItemSummaryDto>();
    }

    /// <summary>
    /// 生成项的摘要信息。
    /// </summary>
    [AutoMap(typeof(GameDiceItemSummary))]
    public class GameDiceItemSummaryDto
    {
        /// <summary>
        /// 生成项的摘要。
        /// </summary>
        public GameEntitySummaryDto Entity { get; set; }

        /// <summary>
        /// 生成项的权重。
        /// </summary>
        public decimal Weight { get; set; }
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
        public List<GameDiceItemSummaryDto> Result { get; set; } = new List<GameDiceItemSummaryDto>();
    }


    #endregion 孵化相关
}


