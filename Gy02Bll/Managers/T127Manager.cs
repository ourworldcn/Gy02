using GY02.Publisher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Manager;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GY02.Managers
{
    public class T127ManagerOptions : IOptions<T127ManagerOptions>
    {
        public T127ManagerOptions Value => this;
    }

    /// <summary>
    /// 
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class T127Manager : GameManagerBase<T127ManagerOptions, T127Manager>
    {
        public T127Manager(IOptions<T127ManagerOptions> options, ILogger<T127Manager> logger, HttpClient httpClient) : base(options, logger)
        {
            _HttpClient = httpClient;
        }

        private readonly HttpClient _HttpClient;

        /// <summary>
        /// 编码过的Code。
        /// </summary>
        public const string _Encode = "4%2F0AfJohXlQpQpxoPOEDj_rSZPy3MTvgOJagDUJzHz--gWOQw12TcnG-UQYWTGwN4zE83vVXQ";

        /// <summary>
        /// 解码后的Code。
        /// </summary>
        public string Code => Uri.UnescapeDataString(_Encode);
        public string _ClientId = "520270928290-96jsrklmrf5ftuqfslsns83aeg8eq44i.apps.googleusercontent.com";
        public string _ClientSecret = "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";
        public string _RedirectUri = "https://developers.google.com";

        /// <summary>
        /// api项目-同意屏幕，发布状态为测试（有效期7天）
        /// RefreshToken 6个月都未使用，这个要维护accessToken的有效性，应该可以不必考虑
        /// 授权账号改密码了（笔者未测试，修改开发者账号密码是否会导致过期）
        /// 授权超过50个刷新令牌，最先的刷新令牌就会失效（这里50个应该够用了，除了测试时，可能会授权多个）
        /// 取消了授权
        /// 属于具有有效会话控制策略的 Google Cloud Platform 组织
        /// </summary>
        public string _RefreshToken = "1//0evSbxh9VRvhvCgYIARAAGA4SNwF-L9IriEbNd4J3zykf3pL3LbcYW70IC3YjD2xhBuIr4ZOeALMS8bO2O1dI7KQblNqD-8Dt6W4";

        /// <summary>
        /// app包名，必须是创建登录api项目时，创建android客户端Id使用包名。
        /// </summary>
        public string _PackageName = "com.animal.animalbump";

        public HttpResponseMessage GetRefreshTokenFromCode(HttpClient client, string code, string clientId, string clientSecret)
        {
            var pa = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("redirect_uri", _RedirectUri),
            });
            var uri = "https://accounts.google.com/o/oauth2/token";
            var result = client.PostAsync(uri, pa).Result;
            return result;
        }

        public virtual HttpResponseMessage GetAccessTokenFromRefreshToken(string refreshToken, string clientId, string clientSecret)
        {
            var pa = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
            });
            var uri = "https://accounts.google.com/o/oauth2/token";
            var result = _HttpClient.PostAsync(uri, pa).Result;
            return result;
        }

        public bool GetAccessTokenFromRefreshToken(string refreshToken, string clientId, string clientSecret, out string accessToken)
        {
            var r = GetAccessTokenFromRefreshToken(refreshToken, clientId, clientSecret);
            GetAccessTokenFromRefreshTokenReturn obj;
            try
            {
                r.EnsureSuccessStatusCode();
                var str1 = r.Content.ReadAsStringAsync().Result;
                obj = JsonSerializer.Deserialize<GetAccessTokenFromRefreshTokenReturn>(str1);
                accessToken = obj.access_token;
                OwHelper.SetLastError(0);
            }
            catch (HttpRequestException)
            {
                accessToken = null;
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_INVALID_DATA, "无法获取访问令牌");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 查询订单状态。
        /// </summary>
        /// <param name="productId">对应购买商品的商品ID。</param>
        /// <param name="token">购买成功后Purchase对象的getPurchaseToken()</param>
        /// <param name="accessToken">获取到的accessToken。省略或为null则自动刷新一个。</param>
        /// 
        /// <returns></returns>
        public virtual HttpResponseMessage GetOrderState(string productId, string token, string accessToken = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                if (!GetAccessTokenFromRefreshToken(_RefreshToken, _ClientId, _ClientSecret, out accessToken))
                    return null;
            }
            var uri = $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{_PackageName}/purchases/products/{productId}/tokens/{token}?access_token={accessToken}";
            //var uri2 = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{packageName}/purchases/products/{productId}/tokens/{token}?access_token={access_token}";
            var result = _HttpClient.GetAsync(uri).Result;
            return result;
        }

        /// <summary>
        /// 获取验证订单的返回信息。
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="token"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public bool GetOrderState(string productId, string token, out T127OrderState result)
        {
            var r = GetOrderState(productId, token);
            if (r is null)
            {
                result = null;
                return false;
            }
            string str;
            try
            {
                str = r.Content.ReadAsStringAsync().Result;
                result = JsonSerializer.Deserialize<T127OrderState>(str);
            }
            catch (Exception err)
            {
                OwHelper.SetLastErrorAndMessage(ErrorCodes.ERROR_INVALID_DATA, err.Message);
                result = null;
                return false;
            }
            OwHelper.SetLastError(0);
            return true;
        }
    }

    public class GetAccessTokenFromRefreshTokenReturn
    {
        public string access_token { get; set; }
    }

    public class T127OrderState
    {
        /*
         * {
  "purchaseTimeMillis": "1623980699933",//购买产品的时间，自纪元（1970 年 1 月 1 日）以来的毫秒数。
  "purchaseState": 0,//订单的购买状态。可能的值为：0. 已购买 1. 已取消 2. 待定
  "consumptionState": 0,//产品的消费状态。可能的值为： 0. 尚未消耗 1. 已消耗
  "developerPayload": "",
  "orderId": "GPA.3398-6726-1036-80298",//google订单号
  "purchaseType": 0,
  "acknowledgementState": 0,
  "kind": "androidpublisher#productPurchase",
  "obfuscatedExternalAccountId": "SDK2106180944530041",//上面客户支付时的透传字段，google指导是用来存放用户信息的，不能过长，否则客户端不能支付
  "obfuscatedExternalProfileId": "",
  "regionCode": "HK"
}
         * */
        /// <summary>
        /// 购买产品的时间，自纪元（1970 年 1 月 1 日）以来的毫秒数。
        /// </summary>
        public string purchaseTimeMillis { get; set; }

        /// <summary>
        /// 订单的购买状态。可能的值为：0. 已购买 1. 已取消 2. 待定
        /// </summary>
        public int purchaseState { get; set; }

        /// <summary>
        /// 产品的消费状态。可能的值为： 0. 尚未消耗 1. 已消耗
        /// </summary>
        public int consumptionState { get; set; }

        public string developerPayload { get; set; }

        /// <summary>
        /// google订单号。
        /// </summary>
        public string orderId { get; set; }

        /// <summary>
        /// purchaseType
        /// </summary>
        public int purchaseType { get; set; }

        public int acknowledgementState { get; set; }

        /// <summary>
        /// androidpublisher#productPurchase
        /// </summary>
        public string kind { get; set; }

        /// <summary>
        /// 上面客户支付时的透传字段，google指导是用来存放用户信息的，不能过长，否则客户端不能支付。
        /// </summary>
        public string obfuscatedExternalAccountId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string obfuscatedExternalProfileId { get; set; }

        /// <summary>
        /// 国家/地区。
        /// </summary>
        public string regionCode { get; set; }
    }

    public static class T127ManagerExtensions
    {
        public static IHttpClientBuilder AddPublisherT127(this IServiceCollection services)
        {
            return services.AddHttpClient<T127Manager, T127Manager>().SetHandlerLifetime(TimeSpan.FromMinutes(5)).ConfigureHttpClient(c =>
            {
                c.DefaultRequestHeaders.Add("ContentType", "application/x-www-form-urlencoded");
            });
        }
    }

}
