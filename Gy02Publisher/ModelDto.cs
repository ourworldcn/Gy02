using OW.Game.Store;
using System.ComponentModel.DataAnnotations;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using OW.Game.Entity;
using System.ComponentModel;

namespace Gy02.Publisher
{
    #region 基础数据结构

#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
    /// <summary>
    /// 错误码封装。
    /// </summary>
    public static class ErrorCodes
    {
        public const int NO_ERROR = 0;

        /// <summary>
        /// 功能未实现。
        /// </summary>
        public const int ERROR_CALL_NOT_IMPLEMENTED = 120;

        /// <summary>
        /// 超时，没有在指定时间内完成操作，通常是锁定超时。
        /// </summary>
        public const int WAIT_TIMEOUT = 258;
        /// <summary>
        /// 无效令牌。
        /// </summary>
        public const int ERROR_INVALID_TOKEN = 315;
        /// <summary>
        /// 找不到指定用户。
        /// </summary>
        public const int ERROR_NO_SUCH_USER = 1317;
        /// <summary>
        /// 并发或交错操作更改了对象的状态，使此操作无效。
        /// </summary>
        public const int E_CHANGED_STATE = unchecked((int)0x8000000C);
        /// <summary>
        /// 未进行身份验证。
        /// </summary>
        public const int Unauthorized = unchecked((int)0x80190191);
        public const int RO_E_CLOSED = unchecked((int)0x80000013);
        /// <summary>
        /// 对象已经被处置。
        /// </summary>
        public const int ObjectDisposed = RO_E_CLOSED;

        /// <summary>
        /// 参数错误。One or more arguments are not correct.
        /// </summary>
        public const int ERROR_BAD_ARGUMENTS = 160;

        /// <summary>
        /// 没有足够的权限来完成请求的操作
        /// </summary>
        public const int ERROR_NO_SUCH_PRIVILEGE = 1313;

        /// <summary>
        /// 没有足够资源完成操作。
        /// </summary>
        public const int RPC_S_OUT_OF_RESOURCES = 1721;

        /// <summary>
        /// 没有足够的配额来处理此命令。通常是超过某些次数的限制。
        /// </summary>
        public const int ERROR_NOT_ENOUGH_QUOTA = 1816;

        /// <summary>
        /// The data is invalid.
        /// </summary>
        public const int ERROR_INVALID_DATA = 13;

        /// <summary>
        /// 操作试图超过实施定义的限制。
        /// </summary>
        public const int ERROR_IMPLEMENTATION_LIMIT = 1292;

        /// <summary>
        /// 无效的账号名称。
        /// </summary>
        public const int ERROR_INVALID_ACCOUNT_NAME = 1315;

        /// <summary>
        /// 指定账号已经存在。
        /// </summary>
        public const int ERROR_USER_EXISTS = 1316;

        /// <summary>
        /// 用户名或密码错误。
        /// </summary>
        public const int ERROR_LOGON_FAILURE = 1326;

        /// <summary>
        /// 无效的ACL——权限令牌包含的权限不足,权限不够。
        /// </summary>
        public const int ERROR_INVALID_ACL = 1336;

        /// <summary>
        /// 无法登录，通常是被封停账号。
        /// </summary>
        public const int ERROR_LOGON_NOT_GRANTED = 1380;

    }
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释

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
    /// 存储一些常量和Id。
    /// </summary>
    public static class ProjectContent
    {
        /// <summary>
        /// 角色的模板Id。
        /// </summary>
        public readonly static Guid CharTId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");
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

    #endregion 基础数据结构

    #region 账号及登录相关

    /// <summary>
    /// 创建角色接口的参数封装类。
    /// </summary>
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
        public string LoginName { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        public string Pwd { get; set; }

        #endregion 可映射属性
    }

    /// <summary>
    /// 
    /// </summary>
    public class LoginReturnDto
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
        public GameChar GameChar { get; set; }

        #endregion 可映射属性
    }
    #endregion 账号及登录相关
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
