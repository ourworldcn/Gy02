﻿using OW.Game.Store;
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

namespace Gy02.Publisher
{
    #region 基础数据结构

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
    }

    /// <summary>
    /// 账号数据。
    /// </summary>
    [AutoMap(typeof(GameUser))]
    public class GameUserDto
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
    public class GameCharDto : GameJsonObjectBase
    {
        #region 简单属性

        /// <summary>
        /// 昵称。
        /// </summary>
        public string DisplayName { get; set; }
        #endregion 简单属性

        #region 各种槽

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
        public GameSlotDto<GameItemDto> ZhuangBeiBag { get; set; }

        /// <summary>
        /// 道具背包。
        /// </summary>
        public GameSlotDto<GameItemDto> DaoJuBag { get; set; }

        /// <summary>
        /// 皮肤背包。
        /// </summary>
        public GameSlotDto<GameItemDto> PiFuBag { get; set; }

        /// <summary>
        /// 货币槽。
        /// </summary>
        public GameSlotDto<GameItemDto> HuoBiSlot { get; set; }
        #endregion 各种槽
    }

    /// <summary>
    /// 道具数据。
    /// </summary>
    [AutoMap(typeof(GameItem))]
    public class GameItemDto : GameJsonObjectBase
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
    public class GameSlotDto<T> : GameJsonObjectBase
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
    public class GameEquipmentDto : GameJsonObjectBase
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
        [JsonPropertyName("pwo")]
        public decimal Pwo { get; set; }

        #endregion 装备数据
    }
    #endregion 基础数据结构

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
}

/*
线程 0x5268 已退出，返回值为 0 (0x0)。
线程 0x5050 已退出，返回值为 0 (0x0)。
线程 0x2c28 已退出，返回值为 0 (0x0)。
线程 0xd90 已退出，返回值为 0 (0x0)。
线程 0x2884 已退出，返回值为 0 (0x0)。
线程 0x1430 已退出，返回值为 0 (0x0)。
线程 0x2c78 已退出，返回值为 0 (0x0)。
线程 0x37dc 已退出，返回值为 0 (0x0)。
线程 0x1948 已退出，返回值为 0 (0x0)。
线程 0x2e5c 已退出，返回值为 0 (0x0)。
线程 0x54cc 已退出，返回值为 0 (0x0)。
线程 0x3d44 已退出，返回值为 0 (0x0)。
线程 0x5290 已退出，返回值为 0 (0x0)。
线程 0x3aa4 已退出，返回值为 0 (0x0)。
线程 0x3c10 已退出，返回值为 0 (0x0)。
线程 0x2bdc 已退出，返回值为 0 (0x0)。
 */