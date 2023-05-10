using GY02.Publisher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GY02.Managers
{

    public class PublisherT78ManagerOptions : IOptions<PublisherT78ManagerOptions>
    {
        public PublisherT78ManagerOptions Value => this;

        /// <summary>
        /// 基础加密矢量
        /// </summary>
        public string AppSecret { get; set; } = "c73f8a6a27cb3e13c4bf455bef422cdb"; //

        /// <summary>
        /// 发行商服务器地址。
        /// </summary>
        public string Url { get; set; } = "https://krm.icebirdgame.com/user/token/v2";
    }

    /// <summary>
    /// T78发行商交互的管理器。
    /// </summary>
    public class PublisherT78Manager : GameManagerBase<PublisherT78ManagerOptions, PublisherT78Manager>
    {
        public PublisherT78Manager(IOptions<PublisherT78ManagerOptions> options, ILogger<PublisherT78Manager> logger, HttpClient httpClient) : base(options, logger)
        {
            _HttpClient = httpClient;
        }

        private readonly HttpClient _HttpClient;

        #region 账号相关

        /// <summary>
        /// 登录账号。
        /// </summary>
        /// <param name="sid"></param>
        /// <returns></returns>
        public T78LoginReturnDto Login(string sid)
        {
            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            var httpResult = PostAsync(new Dictionary<string, string>() { { "sid", sid }, }).Result;
            var resultString = httpResult.Content.ReadAsStringAsync().Result;
            var result = (T78LoginReturnDto)JsonSerializer.Deserialize(resultString, typeof(T78LoginReturnDto), options);
            result.ResultString = resultString;
            return result;
        }

        #endregion 账号相关

        #region 基础功能

        /// <summary>
        /// 发送验证请求。
        /// </summary>
        /// <param name="pairs"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> PostAsync(IReadOnlyDictionary<string, string> pairs)
        {
            var dic = new Dictionary<string, string>(pairs);
            FillDictionary(dic);
            FormUrlEncodedContent content = new FormUrlEncodedContent(dic);
            //content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return _HttpClient.PostAsync(Options.Url, content);
        }

        /// <summary>
        /// 补齐必要字段并添加签名。
        /// </summary>
        /// <param name="pairs"></param>
        public void FillDictionary(IDictionary<string, string> pairs)
        {
            var dic = pairs;
            if (!dic.ContainsKey("gameId"))
                dic["gameId"] = "220000044";

            if (!dic.ContainsKey("channelId"))
                dic["channelId"] = "3";

            if (!dic.ContainsKey("appId"))
                dic["appId"] = "210044001";

            if (!dic.ContainsKey("sid"))
                dic["sid"] = string.Empty;

            if (!dic.ContainsKey("extra"))
                dic["extra"] = string.Empty;

            dic["sign"] = GetSignature(dic as IReadOnlyDictionary<string, string>);
        }

        /// <summary>
        /// 获取签名。
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public string GetSignature(IReadOnlyDictionary<string, string> dic)
        {
            var sb = AutoClearPool<StringBuilder>.Shared.Get();
            using var dw = DisposeHelper.Create(c => AutoClearPool<StringBuilder>.Shared.Return(c), sb);
            byte[] ary;
            foreach (var item in dic.OrderBy(c => c.Key))
            {
                sb.Append(item.Key);
                sb.Append('=');
                sb.Append(item.Value);
            }
            sb.Append(Options.AppSecret);
            ary = Encoding.UTF8.GetBytes(sb.ToString());
            //ary = Encoding.UTF8.GetBytes("appId=channelId=1extra=gameId=1sid=appSecret");

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(ary, 0, ary.Length);
            var result = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            return result;
        }
        #endregion 基础功能

    }

    public static class PublisherT78ManagerExtensions
    {
        public static IHttpClientBuilder AddPublisherT78(this IServiceCollection services)
        {
            return services.AddHttpClient<PublisherT78Manager, PublisherT78Manager>().SetHandlerLifetime(TimeSpan.FromMinutes(5)).ConfigureHttpClient(c =>
            {
                c.DefaultRequestHeaders.Add("ContentType", "application/x-www-form-urlencoded");
            });
        }
    }

}
