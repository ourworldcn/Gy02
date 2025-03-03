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

namespace GY02.Publisher
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
    /// <summary>
    /// 存储一些常量和Id。
    /// </summary>
    public static class ProjectContent
    {
        /// <summary>
        /// 全服配置字典条目的名称。
        /// </summary>
        public const string ServerDictionaryName = "3872287E-E2A3-4D3D-A5FE-C6FF703FA544";

        /// <summary>
        /// 不能通过邮件附件发送物品的标志。
        /// </summary>
        public const string NoMailAttachmentGenus = "flag_NoMailAttachment";

        #region 权限相关

        /// <summary>
        /// 管理员登录名。
        /// </summary>
        public const string AdminLoginName = "0A630B86-0C8F-4CDA-B9BB-A13E35295D71";

        /// <summary>
        /// 管理员初始密码。
        /// </summary>
        public const string AdminPwd = "1954D1C0-5012-44C3-8F1B-0950544862A5";

        /// <summary>
        /// 超管角色标志。拥有所有权限。
        /// </summary>
        public const string SupperAdminRole = "D7A8EA19-2B37-4A70-A80A-708120796093";

        /// <summary>
        /// 管理员角色标志。
        /// </summary>
        public const string AdminRole = "221BBA0F-4DAF-4394-8622-39F4377B61F7";

        /// <summary>
        /// 可发Mail角色标志。
        /// </summary>
        public const string MailAdminRole = "732B7EC2-2319-4055-B762-69E035EB5F5B";

        /// <summary>
        /// 可设置公告角色标志。
        /// </summary>
        public const string BulletinAdminRole = "B5A98E7D-5E18-470E-9CF0-8F8108F6ABB5";

        /// <summary>
        /// 可设置公告角色标志。
        /// </summary>
        public const string QuestionnaireAdminRole = "9C704923-1F4A-41EC-82D6-170CDD281A87";

        /*
        /// <summary>
        /// 可生成兑换码角色标志。
        /// </summary>
        public const string RedeemCodeAdminRole = "2BAD2000-454C-4CD8-B288-4DADB79F68E6";
        */

        #endregion 权限相关

        #region 各种槽和背包

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
        public readonly static Guid YichuanPifuSlotTId = new Guid("18084905-2C57-4B4C-B302-4B8230740FBD");

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
        /// 成就槽TId。
        /// </summary>
        public static readonly Guid ChengJiuSlotTId = new Guid("04AB4C60-9E5B-43A8-894F-3477655371BE");

        /// <summary>
        /// 时装背包TId。
        /// </summary>
        public static readonly Guid ShiZhuangBagTId = new Guid("A92E5EE3-1D48-40A1-BE7F-6C2A9F0BC652");

        /// <summary>
        /// 皮肤背包。3b163a62-6591-4a93-a385-cf079b20f589
        /// </summary>
        public static readonly Guid PiFuBagTId = new Guid("3b163a62-6591-4a93-a385-cf079b20f589");

        /// <summary>
        /// T78注册占位符TId。
        /// </summary>
        public static readonly Guid T78SlotTId = new Guid("7A7A7058-CB88-4D54-80E9-22241774CF51");

        /// <summary>
        /// 形象槽TId。
        /// </summary>
        public static readonly Guid XingxiangSlotTId = new Guid("f1abe4b7-372b-45b2-a9da-eb6930e95cb9");

        /// <summary>
        /// 形象背包TId。
        /// </summary>
        public static readonly Guid XingxiangBagTId = new Guid("f5022e9f-13da-4a2d-b1b9-d57fc07de232");

        /// <summary>
        /// 头像背包TId B39A6FEA-CA36-4C96-975C-59D326EFD7D1
        /// </summary>
        public static readonly Guid TouxiangBagTId = new Guid("B39A6FEA-CA36-4C96-975C-59D326EFD7D1");

        /// <summary>
        /// 头像装备槽TId	3F38C631-1694-41F5-BA70-7D25900D4D96
        /// </summary>
        public static readonly Guid TouxiangSlotTId = new Guid("3F38C631-1694-41F5-BA70-7D25900D4D96");

        #endregion 各种槽和背包

        #region 邮件相关

        /// <summary>
        /// 邮件槽TId。
        /// </summary>
        public static readonly Guid MailSlotTId = new Guid("CE00523E-9060-4D74-96C7-C7D3D57CE650");

        /// <summary>
        /// 邮件槽TId。（收件）
        /// </summary>
        public static readonly Guid MailTId = new Guid("E7229A9E-23A4-42EF-8ABB-FAEF0E16F683");

        #endregion

        /// <summary>
        /// 法币TID。
        /// </summary>
        public static readonly Guid FabiTId = new Guid("d577e084-4aa4-44d9-9510-20ccea041375");

        /// <summary>
        /// 广告币TID。
        /// </summary>
        public static readonly Guid GuanggaoCurrenyTId = new Guid("1de88b53-e5bf-4a60-9dfb-90284b94bdab");

        /// <summary>
        /// 金币TID。
        /// </summary>
        public static readonly Guid GoldTId = new Guid("a45b3421-3688-43c5-b8f5-429db7621f69");

        /// <summary>
        /// 钻石TID。
        /// </summary>
        public static readonly Guid DiamTId = new Guid("c9575f24-a33d-49ba-b130-29b6ff4d62c7");

        /// <summary>
        /// 通关币TID。
        /// </summary>
        public static readonly Guid TongguanBiTId = new Guid("49597D35-877B-420F-BA2F-121FB2D4F63F");

        /// <summary>
        /// 体力TID。
        /// </summary>
        public static readonly Guid PowerTId = new Guid("f1b15c9e-32ee-4203-af62-d324bfa4f7e7");

        /// <summary>
        /// 孵化次数货币的TId。
        /// </summary>
        public static readonly Guid FuhuaCishuTId = new Guid("9EBDC71E-F2AD-4F1C-82B9-88955FE15B4B");

        /// <summary>
        /// 累计签到占位符的TId。
        /// </summary>
        public static readonly Guid LeijiQiandaoSlotTId = new Guid("4527471A-FD91-4899-910C-DE7056AD67A1");

        /// <summary>
        /// 7日签到标志的TId。
        /// </summary>
        public static readonly Guid SevenDayQiandaoSlotTId = new Guid("46542DE4-B8B8-4735-936C-856273B650F7");

        /// <summary>
        /// 累计登录天数的TId。
        /// </summary>
        public static readonly Guid LoginedDayTId = new Guid("24DD1BA1-628F-4F04-BDAF-4C9B0CEF895C");

        /// <summary>
        /// 巡逻币。
        /// </summary>
        public static readonly Guid XunluoTId = new Guid("62D3A545-7604-46BF-9837-95E286660BC8");

        /// <summary>
        /// 周卡占位符。
        /// </summary>
        public static readonly Guid ZhoukaTId = new Guid("91E6DD20-1F00-4090-9A26-EA04B7693E60");

        /// <summary>
        /// 月卡占位符。
        /// </summary>
        public static readonly Guid YuekaTId = new Guid("419A400B-55A3-4244-82A6-1917267654F7");

        /// <summary>
        /// 七日签到页签。
        /// </summary>
        public const string QiriQiandaoGenus = "gs_qiandao";

        /// <summary>
        /// 法币购买项的页签。
        /// </summary>
        public const string CurrencyBuyGenus = "fb_goumai";

        /// <summary>
        /// 此类实体在每天第一次登录时会自动把Count置为该实体存在的总天数，从0开始。副作用，此类属实体的Count设置由系统完成单独设置无用。fl_ExistsDayNumber
        /// </summary>
        public const string ExistsDayNumberGenus = "fl_ExistsDayNumber";

        /// <summary>
        /// 每日递增。
        /// </summary>
        public const string AutoIncGenus = "fl_AutoInc";

        /// <summary>
        /// 关卡结算后看广告额外产出占位符。
        /// </summary>
        public static readonly Guid AdsCombatTid = new Guid("ED31E2D9-5D01-4AC9-BB23-91AA1D756DD9");

        #region 游戏内实体类型的类型Guid

        /// <summary>
        /// 槽类型的类型Guid。
        /// </summary>
        public static readonly Guid SlotTypeGuid = new Guid("57EB05BD-B8BE-47D5-ADFE-C8C2E8888E38");
        #endregion 游戏内实体类型的类型Guid
    }

    /// <summary>
    /// 错误码封装。
    /// </summary>
    /// <remarks>https://learn.microsoft.com/zh-cn/windows/win32/debug/system-error-codes</remarks>
    public static class ErrorCodes
    {
        /// <summary>
        /// 获取错误码的操作系统语言说明。
        /// </summary>
        /// <param name="error"></param>
        /// <returns>一个错误的说明，与运行操作系统的语言一致。</returns>
        public static string GetMessage(int error) => new System.ComponentModel.Win32Exception(error).Message;

        public const int NO_ERROR = 0;

        /// <summary>
        /// The data is invalid.
        /// </summary>
        public const int ERROR_INVALID_DATA = 13;
        /// <summary>
        /// 功能未实现。
        /// </summary>
        public const int ERROR_CALL_NOT_IMPLEMENTED = 120;

        /// <summary>
        /// 参数错误。One or more arguments are not correct.
        /// </summary>
        public const int ERROR_BAD_ARGUMENTS = 160;


        /// <summary>
        /// 超时，没有在指定时间内完成操作，通常是锁定超时。
        /// </summary>
        public const int WAIT_TIMEOUT = 258;

        /// <summary>
        /// 无效令牌。
        /// </summary>
        public const int ERROR_INVALID_TOKEN = 315;

        /// <summary>
        /// 没有足够的权限来完成请求的操作
        /// </summary>
        public const int ERROR_NO_SUCH_PRIVILEGE = 1313;
        /// <summary>
        /// 找不到指定用户。
        /// </summary>
        public const int ERROR_NO_SUCH_USER = 1317;

        /// <summary>
        /// 用户名或密码不正确。
        /// </summary>
        public const int ERROR_LOGON_FAILURE = 1326;

        /// <summary>
        /// 用户帐户限制阻止了此用户进行登录。例如：不允许使用空密码，登录次数的限制，或强制实施的某个策略限制。
        /// </summary>
        public const int ERROR_ACCOUNT_RESTRICTION = 1327;

        /// <summary>
        /// 没有足够资源完成操作。
        /// </summary>
        public const int RPC_S_OUT_OF_RESOURCES = 1721;

        /// <summary>
        /// 没有足够的配额来处理此命令。通常是超过某些次数的限制。
        /// </summary>
        public const int ERROR_NOT_ENOUGH_QUOTA = 1816;

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
        /// 无效的ACL——权限令牌包含的权限不足,权限不够。
        /// </summary>
        public const int ERROR_INVALID_ACL = 1336;

        /// <summary>
        /// 无法登录，通常是被封停账号。
        /// </summary>
        public const int ERROR_LOGON_NOT_GRANTED = 1380;

        /// <summary>
        /// 并发或交错操作更改了对象的状态，使此操作无效。
        /// </summary>
        public const int E_CHANGED_STATE = unchecked((int)0x8000000C);

        /// <summary>
        /// 未进行身份验证。
        /// </summary>
        public const int Unauthorized = unchecked((int)0x80190191);

        /// <summary>
        /// 必须的资源已经被关闭。
        /// </summary>
        public const int RO_E_CLOSED = unchecked((int)0x80000013);

        /// <summary>
        /// 对象已经被处置。
        /// </summary>
        public const int ObjectDisposed = RO_E_CLOSED;

    }
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释

}
