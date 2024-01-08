using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GY02.Managers
{
    /// <summary>
    /// T1228合作伙伴的管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class T1228Manager : GameManagerBase<T1228ManagerOptions, T1228Manager>
    {
        public T1228Manager(IOptions<T1228ManagerOptions> options, ILogger<T1228Manager> logger, HttpClient httpClient) : base(options, logger)
        {
            _HttpClient = httpClient;
        }

        private readonly HttpClient _HttpClient;

        /// <summary>
        /// sdk服务器使用域名地址。
        /// </summary>
        public const string SdkServerUrl = "api-sdk-gameplus.meetsocial.com";

        /// <summary>
        /// 获取用户信息的Url。
        /// </summary>
        const string _GetUserInfoUrl = $"https://{SdkServerUrl}/auth/myProfile";

        /// <summary>
        /// 回调地址。
        /// </summary>
        const string _PayedCallback = "https://sa.meetsocial.1stlightstudio.com:20443/api/T1228/Payed1228";

        /*
         * AppID: 514540092563308819
         * AppKey: CNOjqhub75t3mbmYtmKJEE4J8i8oaBg2Erz8
         * AppSecret: YDjCiVmvo8KJnGCwoKZ5EpyemwR6XWt8x0bR
         */

        /// <summary>
        /// 应用Id。
        /// </summary>
        public const string AppID = "514540092563308819";

        public const string AppKey = "CNOjqhub75t3mbmYtmKJEE4J8i8oaBg2Erz8";

        /// <summary>
        /// 生成签名的密钥。
        /// </summary>
        public const string AppSecret = "YDjCiVmvo8KJnGCwoKZ5EpyemwR6XWt8x0bR";

        /// <summary>
        /// 获取用户信息。
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public T1228GetUserInfo GetUserInfo(string token)
        {
            _HttpClient.DefaultRequestHeaders.Remove("Authorization");
            _HttpClient.DefaultRequestHeaders.Add("Authorization", $"{token}");
            var str = _HttpClient.GetStringAsync(_GetUserInfoUrl).Result;
            return JsonSerializer.Deserialize<T1228GetUserInfo>(str);
        }

        /// <summary>
        /// 获取验证的字符串。
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public string GetString(Payed1228ParamsDto dto)
        {
            StringBuilder sb = new StringBuilder();
            var type = dto.GetType();
            var pis = type.GetProperties();
            foreach (var item in dto.signOrder)
            {
                var pi = pis.FirstOrDefault(c => c.Name == item);
                object val;
                if (pi is null)
                {
                    if (dto.ExtensionData.TryGetValue(item, out var obj))
                        val = obj.ToString();
                    else
                        throw new InvalidOperationException("无法找到需要排序的字段。");
                }
                else
                    val = pi.GetValue(dto);
                sb.Append($"{item}={val?.ToString()}&");
            }
            sb.Append($"secret={AppSecret}");
            return sb.ToString();
        }

        /// <summary>
        /// 获取签名后的字符串。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetSign(string str)
        {
            var tmp = Encoding.UTF8.GetBytes(str);
            var md5 = MD5.HashData(tmp);
            var result = Convert.ToBase64String(md5);
            Debug.Assert(result[^2..] == "==");
            return result;
        }

    }

    public class T1228GetUserInfo
    {
        /// <summary>
        /// 状态码，200以外均为异常。
        /// </summary>
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// 错误类型。
        /// </summary>
        public string error { get; set; }

        /// <summary>
        /// 错误描述。
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// UserProfile对象.
        /// </summary>
        [JsonPropertyName("data")]
        public T1228UserProfile Data { get; set; }

        /// <summary>
        /// 追踪Id.
        /// </summary>
        public string TraceId { get; set; }
    }

    public class T1228UserProfile
    {
        /// <summary>
        /// 平台应用下用户的唯一id。
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 唯一用户账户名。
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 是否游客（未绑定第三方账号为游客）
        /// </summary>
        public bool isGuest { get; set; }

        /// <summary>
        /// 是否同意协议。
        /// </summary>
        public bool agreementChecked { get; set; }
    }

    public static class T1228ManagerExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IHttpClientBuilder AddPublisherT1228(this IServiceCollection services)
        {
            return services.AddHttpClient<T1228Manager, T1228Manager>().SetHandlerLifetime(TimeSpan.FromMinutes(5)).ConfigureHttpClient(c =>
            {
                c.DefaultRequestHeaders.Add("ContentType", "application/json");
            });
        }

    }

    /// <summary>
    /// 支付回调接口的参数封装类。
    /// </summary>
    public class Payed1228ParamsDto
    {
        /// <summary>
        /// 签名字段顺序列表。
        /// </summary>
        public List<string> signOrder { get; set; }

        /// <summary>
        /// 商品类型，应用平台
        /// </summary>
        public string productType { get; set; }

        /// <summary>
        /// 各平台配置的应用内唯一编码
        /// </summary>
        public string productCode { get; set; }

        /// <summary>
        /// 第三方平台生成的原始订单号
        /// </summary>
        public string originOrderId { get; set; }

        /// <summary>
        /// 第三方原始订单回调数据
        /// </summary>
        public string originInfo { get; set; }

        /// <summary>
        /// 订单唯一编号
        /// </summary>
        public string orderId { get; set; }

        /// <summary>
        /// 固定值orderPayed。
        /// </summary>
        public string @event { get; set; }

        /// <summary>
        /// 用户上传的订单数据。
        /// </summary>
        public string customInfo { get; set; }

        /// <summary>
        /// 创建时间.
        /// </summary>
        public string createTime { get; set; }

        /// <summary>
        /// 应用唯一编号
        /// </summary>
        public string appId { get; set; }

        /// <summary>
        /// 签名。
        /// </summary>
        public string sign { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// customInfo参数类型。
    /// </summary>
    public class T1218PayedCustomInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public T1218PayedCustomInfo()
        {

        }

        /// <summary>
        /// 商品ID	
        /// </summary>
        public string productId { get; set; }

        /// <summary>
        /// 商品类型
        /// </summary>
        public string productType { get; set; }

        /// <summary>
        /// 透传参数(最多250字符)
        /// </summary>
        public string developerPayload { get; set; }

        /// <summary>
        /// 角色信息	
        /// </summary>
        public T1218PayedRoleInfo roleInfo { get; set; }
    }

    /// <summary>
    /// roleInfo参数说明.
    /// </summary>
    public class T1218PayedRoleInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public T1218PayedRoleInfo()
        {

        }

        /// <summary>
        /// 角色ID
        /// </summary>
        public string roleId { get; set; }

        /// <summary>
        /// 角色名	
        /// </summary>
        public string roleName { get; set; }

        /// <summary>
        /// 角色等级	
        /// </summary>
        public string roleLevel { get; set; }

        /// <summary>
        /// 服务器名称	
        /// </summary>
        public string serverName { get; set; }

        /// <summary>
        /// vip等级	
        /// </summary>
        public string vipLevel { get; set; }
    }

    /// <summary>
    /// T1228合作伙伴的管理器的配置类。
    /// </summary>
    public class T1228ManagerOptions : IOptions<T1228ManagerOptions>
    {
        public T1228ManagerOptions()
        {
        }

        T1228ManagerOptions IOptions<T1228ManagerOptions>.Value => this;
    }

}
