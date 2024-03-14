using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GY02.Managers
{
    public class T0314ManagerOptions : IOptions<T0314ManagerOptions>
    {
        public T0314ManagerOptions Value => this;
    }

    /// <summary>
    /// T0314合作伙伴的管理器。
    /// https://quick.shfoga.com/docs/index/aid/512#catlog2帮助地址。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class T0314Manager : GameManagerBase<T0314ManagerOptions, T0314Manager>
    {
        /// <summary>
        /// 捷游/东南亚相关功能管理器的构造函数。
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public T0314Manager(IOptions<T0314ManagerOptions> options, ILogger<T0314Manager> logger, HttpClient httpClient) : base(options, logger)
        {
            _HttpClient = httpClient;
            var str = Path.Combine(SdkServerUrl, LoginUrl);
        }

        /// <summary>
        /// SDK服务器调用地址。
        /// </summary>
        public const string SdkServerUrl = "https://quick-api.shfoga.com/webapi/";
        public const string LoginUrl = "checkUserInfo";

        private readonly HttpClient _HttpClient;

        public T0314LoginReturn Login(string token, string uid)
        {
            var builder = new UriBuilder(Path.Combine(SdkServerUrl, LoginUrl));
            builder.Query = $"token={token}&uid={uid}";
            var str = _HttpClient.GetStringAsync(builder.Uri).Result;
            return JsonSerializer.Deserialize<T0314LoginReturn>(str);
        }
    }

    public class T0314LoginReturn
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public object Data { get; set; }

        [JsonPropertyName("uid")]
        public string Uid { get; set; }
    }

    public static class T0314ManagerExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IHttpClientBuilder AddPublisherT0314(this IServiceCollection services)
        {
            return services.AddHttpClient<T1228Manager, T1228Manager>().SetHandlerLifetime(TimeSpan.FromMinutes(5)).ConfigureHttpClient(c =>
            {
                c.DefaultRequestHeaders.Add("ContentType", "application/json");
            });
        }

    }


}
