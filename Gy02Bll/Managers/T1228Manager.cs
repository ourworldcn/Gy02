using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GY02.Managers
{

    public class T1228ManagerOptions : IOptions<T1228ManagerOptions>
    {
        public T1228ManagerOptions()
        {
        }

        T1228ManagerOptions IOptions<T1228ManagerOptions>.Value => this;
    }

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

        const string _GetUserInfoUrl = $"https://{SdkServerUrl}/auth/myProfile";

        /// <summary>
        /// 回调地址。
        /// </summary>
        const string _PayedCallback = "https://sa.meetsocial.1stlightstudio.com:20443/api/T1228/Payed1228";

        public T1228GetUserInfo GetUserInfo(string token)
        {
            _HttpClient.DefaultRequestHeaders.Remove("Authorization");
            _HttpClient.DefaultRequestHeaders.Add("Authorization", $"{token}");
            var str = _HttpClient.GetStringAsync(_GetUserInfoUrl).Result;
            return JsonSerializer.Deserialize<T1228GetUserInfo>(str);
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
}
