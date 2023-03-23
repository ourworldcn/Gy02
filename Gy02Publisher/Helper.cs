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
        /// 账号的模板Id。
        /// </summary>
        public readonly static Guid UserTId = new Guid("dc58d091-623a-4d7d-b9e5-b645e87d4e79");

        /// <summary>
        /// 角色的模板Id。
        /// </summary>
        public readonly static Guid CharTId = new Guid("07664462-df05-4ba7-886d-b431bb88aa1c");

        /// <summary>
        /// 武器槽TId。
        /// </summary>
        public readonly static Guid WuQiSlotTId = new Guid("29b7e726-387f-409d-a6ac-ad8670a814f0");

        /// <summary>
        /// 手套槽TId。
        /// </summary>
        public static readonly Guid ShouTaoSlotTId = new Guid("4d125986-4a3f-4294-bf54-a70853d719c1");

        /// <summary>
        /// 衣服槽TId。
        /// </summary>
        public static readonly Guid YiFuSlotTId = new Guid("3d141082-0deb-4f59-ac5d-a80e43657ccc");

        /// <summary>
        /// 鞋子槽TId。
        /// </summary>
        public static readonly Guid XieZiSlotTId = new Guid("fe312261-2233-4c35-aaf4-bd698e328baf");

        /// <summary>
        /// 腰带槽TId。
        /// </summary>
        public static readonly Guid YaoDaiSlotTId = new Guid("af7b7b9b-a881-4cbe-bca5-111f7babd73a");

        /// <summary>
        /// 坐骑槽TId。
        /// </summary>
        public static readonly Guid ZuoJiSlotTId = new Guid("14d0e372-909b-485f-b8cb-07c9231b10ff");

        /// <summary>
        /// 装备背包TId。
        /// </summary>
        public static readonly Guid ZhuangBeiBagTId = new Guid("e6edab87-0034-4f45-8040-1f0d565ccf58");

        /// <summary>
        /// 道具背包TId。
        /// </summary>
        public static readonly Guid DaoJuBagTId = new Guid("9c559895-9a2c-43a7-b793-e879a600487c");

        /// <summary>
        /// 货币槽TId。
        /// </summary>
        public static readonly Guid HuoBiSlotTId = new Guid("123a5ad1-d4f0-4cd9-9abc-d440419d9e0d");

        /// <summary>
        /// 时装背包TId。
        /// </summary>
        public static readonly Guid ShiZhuangBagTId = new Guid("A92E5EE3-1D48-40A1-BE7F-6C2A9F0BC652");

        /// <summary>
        /// 皮肤背包。
        /// </summary>
        public static readonly Guid PiFuBagTId = new Guid("3b163a62-6591-4a93-a385-cf079b20f589");
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

    /// <summary>
    /// 类型映射。
    /// </summary>
    public static class TypeTable
    {
        //public Dictionary<Guid, (Type, Type)> Types { get; set; } = new Dictionary<Guid, (Type, Type)>() { { Guid.NewGuid, (null, null) } };
    }

}
