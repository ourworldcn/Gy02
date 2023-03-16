using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gy02.Publisher
{

#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
    /// <summary>
    /// 存储一些常量和Id。
    /// </summary>
    public static class ProjectContent
    {
        /// <summary>
        /// 角色的模板Id。
        /// </summary>
        public readonly static Guid CharTId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");

        /// <summary>
        /// 角色的模板Id。
        /// </summary>
        public readonly static Guid UserTId = new Guid("def2fbc1-0928-4e78-b74e-edb20c1c9eb3");
    }

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
    /// 帮助器类。
    /// </summary>
    public static class ServerHelper
    {
    }

}
