using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
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

        /// <summary>
        /// 验证签名的Key。
        /// </summary>
        public const string OpenKey = "iOnaKGcn1X0aQTfzEFpCm1yehPJADlBW";

        public const string IosCallbackKey = "13910753290173445245276973738589";

        public const string AndroidCallbackKey = "84989153729002945222447943365883";

        public T0314LoginReturn Login(string token, string uid)
        {
            var builder = new UriBuilder(Path.Combine(SdkServerUrl, LoginUrl));
            builder.Query = $"token={token}&uid={uid}";
            var str = _HttpClient.GetStringAsync(builder.Uri).Result;
            return JsonSerializer.Deserialize<T0314LoginReturn>(str);
        }

        /// <summary>
        /// 获取签名。
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public byte[] GetSignForAndroid(IDictionary<string, string> dic)
        {
            //得到字符串
            var sb = new StringBuilder();
            foreach (var item in dic.Where(c => c.Key != "sign").OrderBy(c => c.Key))
            {
                sb.Append(item.Key);
                sb.Append('=');
                sb.Append(item.Value);
                sb.Append('&');
            }
            sb.Remove(sb.Length - 1, 1);    //去掉&号
            var str = sb.ToString();
            Logger.LogInformation("要签名的字符串是 {str}", str);
            //计算签名
            var result = GetSignForAndroid(str);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str">不含& 与 回调key的拼接字符串。</param>
        /// <returns></returns>
        public byte[] GetSignForAndroid(string str)
        {
            var bin = Encoding.UTF8.GetBytes(str + "&" + AndroidCallbackKey);
            var result = MD5.HashData(bin);
            return result;
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

    /// <summary>
    /// 支付回调参数封装类。
    /// </summary>
    /// <remarks>注：如果是官网充值方式时，CP可以获取extrasParams参数的值来定位到角色信息。这里的值CP可与第三方官网约定一个规则，让第三方官网按照规则传值。
    /// 目前规则为:区服ID|@|角色ID|@|商品ID</remarks>
    [AutoMapper.AutoMap(typeof(Dictionary<string, string>), ReverseMap = true)]
    public class T0314PayReturnDto
    {
        public T0314PayReturnDto()
        {

        }

        /// <summary>
        /// 购买道具的用户uid。
        /// </summary>
        [JsonPropertyName("uid")]
        public string Uid { get; set; }

        /// <summary>
        /// 购买道具的用户username。
        /// </summary>
        [JsonPropertyName("username")]
        public string UserName { get; set; }

        /// <summary>
        /// 游戏下单时传递的游戏订单号，原样返回。
        /// </summary>
        [JsonPropertyName("cpOrderNo")]
        public string CpOrderNo { get; set; }

        /// <summary>
        /// SDK唯一订单号。
        /// </summary>
        [JsonPropertyName("orderNo")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 用户支付时间，如2017-02-06 14:22:32
        /// </summary>
        [JsonPropertyName("payTime")]
        public DateTime PayTime { get; set; }

        /// <summary>
        /// 订单支付方式，具体值对应支付渠道详见对照表.
        /// </summary>
        [JsonPropertyName("payType")]
        public string PayType { get; set; }

        /// <summary>
        /// 用户支付金额（单位：元）,注意：如果游戏商品有多个数量，那么金额就是单价*数量
        /// </summary>
        [JsonPropertyName("payAmount")]
        public decimal PayAmount { get; set; }

        /// <summary>
        /// 用户支付的币种，如RMB，USD等.
        /// </summary>
        [JsonPropertyName("payCurrency")]
        public string PayCurrency { get; set; }

        /// <summary>
        /// 用户支付的游戏道具以美元计价的金额（单位：元）.注意：如果游戏商品有多个数量，那么金额就是单价*数量
        /// </summary>
        [JsonPropertyName("usdAmount")]
        public decimal UsdAmount { get; set; }

        /// <summary>
        /// 支付状态，为0表示成功，为1时游戏不做处理
        /// </summary>
        [JsonPropertyName("payStatus")]
        public int PayStatus { get; set; }

        /// <summary>
        /// 充值折扣，取值范围0~1(不包含0），默认为1表示不折扣；如值为0.2表示多发20%的元宝
        /// </summary>
        [JsonPropertyName("actRate")]
        public decimal ActRate { get; set; }

        /// <summary>
        /// 游戏下单时传递的扩展参数，将原样返回。
        /// </summary>
        [JsonPropertyName("extrasParams")]
        public string ExtrasParams { get; set; }

        /// <summary>
        /// 内购订阅型商品订单使用，如果有此字段表示订单订阅状态。cp监测到有此字段时不需要发货。字段取值为：2：订阅取消
        /// </summary>
        [JsonPropertyName("subscriptionStatus")]
        public int? SubscriptionStatus { get; set; }

        /// <summary>
        /// 内购订阅型商品订单取消订阅原因。当有subscriptionStatus字段时此字段必有
        /// </summary>
        [JsonPropertyName("subReason")]
        public string SubReason { get; set; }

        /// <summary>
        /// 签名值，游戏应根据签名约定，本地计算后与此值进行比对
        /// </summary>
        [JsonPropertyName("sign")]
        public string Sign { get; set; }

        public Dictionary<string, string> GetDic()
        {
            var dic = new Dictionary<string, string>();
            var coll = GetType().GetProperties().Select(c =>
            {
                var name = c.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? c.Name;
                return (name, c.GetValue(this)?.ToString());
            });
            return coll.ToDictionary(c => c.name, c => c.Item2);
        }
    }

    [AutoMapper.AutoMap(typeof(Dictionary<string, string>), ReverseMap = true)]
    public class T0314PayReturnStringDto
    {
        public T0314PayReturnStringDto()
        {

        }

        /// <summary>
        /// 购买道具的用户uid。
        /// </summary>
        [JsonPropertyName("uid")]
        public string Uid { get; set; }

        /// <summary>
        /// 购买道具的用户username。
        /// </summary>
        [JsonPropertyName("username")]
        public string UserName { get; set; }

        /// <summary>
        /// 游戏下单时传递的游戏订单号，原样返回。
        /// </summary>
        [JsonPropertyName("cpOrderNo")]
        public string CpOrderNo { get; set; }

        /// <summary>
        /// SDK唯一订单号。
        /// </summary>
        [JsonPropertyName("orderNo")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 用户支付时间，如2017-02-06 14:22:32
        /// </summary>
        [JsonPropertyName("payTime")]
        public string PayTime { get; set; }

        /// <summary>
        /// 订单支付方式，具体值对应支付渠道详见对照表.
        /// </summary>
        [JsonPropertyName("payType")]
        public string PayType { get; set; }

        /// <summary>
        /// 用户支付金额（单位：元）,注意：如果游戏商品有多个数量，那么金额就是单价*数量
        /// </summary>
        [JsonPropertyName("payAmount")]
        public string PayAmount { get; set; }

        /// <summary>
        /// 用户支付的币种，如RMB，USD等.
        /// </summary>
        [JsonPropertyName("payCurrency")]
        public string PayCurrency { get; set; }

        /// <summary>
        /// 用户支付的游戏道具以美元计价的金额（单位：元）.注意：如果游戏商品有多个数量，那么金额就是单价*数量
        /// </summary>
        [JsonPropertyName("usdAmount")]
        public string UsdAmount { get; set; }

        /// <summary>
        /// 支付状态，为0表示成功，为1时游戏不做处理
        /// </summary>
        [JsonPropertyName("payStatus")]
        public string PayStatus { get; set; }

        /// <summary>
        /// 充值折扣，取值范围0~1(不包含0），默认为1表示不折扣；如值为0.2表示多发20%的元宝
        /// </summary>
        [JsonPropertyName("actRate")]
        public string ActRate { get; set; }

        /// <summary>
        /// 游戏下单时传递的扩展参数，将原样返回。
        /// </summary>
        [JsonPropertyName("extrasParams")]
        public string ExtrasParams { get; set; }

        /// <summary>
        /// 内购订阅型商品订单使用，如果有此字段表示订单订阅状态。cp监测到有此字段时不需要发货。字段取值为：2：订阅取消
        /// </summary>
        [JsonPropertyName("subscriptionStatus")]
        public string SubscriptionStatus { get; set; }

        /// <summary>
        /// 内购订阅型商品订单取消订阅原因。当有subscriptionStatus字段时此字段必有
        /// </summary>
        [JsonPropertyName("subReason")]
        public string SubReason { get; set; }

        /// <summary>
        /// 签名值，游戏应根据签名约定，本地计算后与此值进行比对
        /// </summary>
        [JsonPropertyName("sign")]
        public string Sign { get; set; }

        public Dictionary<string, string> GetDic()
        {
            var dic = new Dictionary<string, string>();
            var coll = GetType().GetProperties().Select(c =>
            {
                var name = c.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? c.Name;
                return (name, c.GetValue(this)?.ToString());
            });
            return coll.ToDictionary(c => c.name, c => c.Item2);
        }
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
