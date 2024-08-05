using GY02.Commands;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GY02.Managers
{
    public class T0314ManagerOptions : IOptions<T0314ManagerOptions>
    {
        public T0314ManagerOptions Value => this;
    }

    /// <summary>
    /// T0314合作伙伴的管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class T0314Manager : GameManagerBase<T0314ManagerOptions, T0314Manager>
    {
        /// <summary>
        /// 捷游/东南亚相关功能管理器的构造函数。
        /// https://quick.shfoga.com/docs/index/aid/512#catlog2帮助地址。
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public T0314Manager(IOptions<T0314ManagerOptions> options, ILogger<T0314Manager> logger, HttpClient httpClient, IDbContextFactory<GY02UserContext> dbContextFactory,
            GameAccountStoreManager accountStore, GameTemplateManager templateManager, GameShoppingManager shoppingManager, SpecialManager specialManager,
            IServiceProvider service, IHostEnvironment environment, GameEntityManager entityManager)
            : base(options, logger)
        {
            _HttpClient = httpClient;
            var str = Path.Combine(SdkServerUrl, LoginUrl);
            _DbContextFactory = dbContextFactory;
            _AccountStore = accountStore;
            _TemplateManager = templateManager;
            _ShoppingManager = shoppingManager;
            _SpecialManager = specialManager;
            _Service = service;
            _Environment = environment;
            _EntityManager = entityManager;
        }

        IServiceProvider _Service;
        IDbContextFactory<GY02UserContext> _DbContextFactory;
        GameAccountStoreManager _AccountStore;
        GameTemplateManager _TemplateManager;
        GameShoppingManager _ShoppingManager;
        SpecialManager _SpecialManager;
        IHostEnvironment _Environment;
        GameEntityManager _EntityManager;

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
        public byte[] GetSignForAndroid(IReadOnlyDictionary<string, string> dic)
        {
            var str = GetSignString(dic);
            Logger.LogInformation("要签名的字符串是 {str}", str);
            //计算签名
            var result = GetSignForAndroid(str);
            return result;
        }

        /// <summary>
        /// 获取签名。
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public byte[] GetSignForIos(IReadOnlyDictionary<string, string> dic)
        {
            var str = GetSignString(dic);
            Logger.LogInformation("要签名的字符串是 {str}", str);
            //计算签名
            var result = GetSignForIos(str);
            return result;
        }

        /// <summary>
        /// 获取代签名字符串。不含签名的key。
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public string GetSignString(IReadOnlyDictionary<string, string> dic)
        {
            var sb = AutoClearPool<StringBuilder>.Shared.Get();
            try
            {
                foreach (var item in dic.Where(c => c.Key != "sign").OrderBy(c => c.Key))
                {
                    sb.Append(item.Key);
                    sb.Append('=');
                    sb.Append(item.Value);
                    sb.Append('&');
                }
                sb.Remove(sb.Length - 1, 1);    //去掉&号
                return sb.ToString();
            }
            finally
            {
                AutoClearPool<StringBuilder>.Shared.Return(sb);
            }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public byte[] GetSignForIos(string str)
        {
            var bin = Encoding.UTF8.GetBytes(str + "&" + IosCallbackKey);
            var result = MD5.HashData(bin);
            return result;
        }

        /// <summary>
        /// 该订单在未来需要扫描。
        /// </summary>
        /// <param name="orderId"></param>
        public void Reg(Guid orderId)
        {
            CancellationTokenSource cts = new CancellationTokenSource(60_000);
            cts.Token.Register(c =>
            {
                if (c is not Guid orderId) return;
                using var db = _DbContextFactory.CreateDbContext();
                if (db.ShoppingOrder.Find(orderId) is not GameShoppingOrder tmp) return;
                if (!Guid.TryParse(tmp.CustomerId, out var gcId)) return;

                var key = _AccountStore.GetKeyByCharId(gcId, db);
                using var dw = _AccountStore.GetOrLoadUser(key, out var gu);
                if (dw.IsEmpty) return;
                var gc = gu.CurrentChar;

                if (db.ShoppingOrder.FirstOrDefault(c => c.Id == orderId) is not GameShoppingOrder order) return;
                if (!order.Confirm1 || !order.Confirm2) return;
                order.State = 1;
                var jo = order.GetJsonObject<T0314JObject>();
                if (!jo.IsClientCreate || jo.SendInMail is not null) return;
                //分拣发送及直接放置物品
                List<GameEntitySummary> forMail = new List<GameEntitySummary>();
                List<GameEntitySummary> noMail = new List<GameEntitySummary>();
                foreach (var item in jo.EntitySummaries)
                {
                    var ttTmp = _TemplateManager.GetFullViewFromId(item.TId);
                    if (ttTmp.Genus?.Contains(ProjectContent.NoMailAttachmentGenus) ?? false)  //若直接放置
                    {
                        noMail.Add(item);
                    }
                    else //若发送邮件
                    {
                        forMail.Add(item);
                    }
                }
                //准备发送邮件
                #region 发送奖品邮件
                if (forMail.Count > 0)
                {
                    using var scope = _Service.CreateScope();
                    var _SyncCommandManager = scope.ServiceProvider.GetService<SyncCommandManager>();

                    var commandMail = new SendMailCommand
                    {
                        GameChar = gc,
                        Mail = new SendMailItem
                        {
                            Subject = "Please check receipt of goods",
                            Body = "Hero, we found that your purchased item was not obtained in a timely manner. We are now resending it to you. Best of luck to you.",
                        },
                    };
                    commandMail.Mail.Dictionary1 = new Dictionary<string, string>()
                {
                    { "English","Please check receipt of goods"},
                    { "Chinese","商品请查收"},
                    { "Filipino","Maaring i-check ang inyong natanggap na produkto"},
                    { "Indonesian","Silakan periksa enerimaan barang"},
                    { "Malay","Sila semak penerimaan barang."},
                    { "Thai","กรุณาตรวจรับสินค้า"},
                };
                    commandMail.Mail.Dictionary2 = new Dictionary<string, string>()
                {
                    { "English","Hero, we found that your purchased item was not obtained in a timely manner. We are now resending it to you. Best of luck to you."},
                    { "Chinese","英雄，由于查询到，你购买的商品未及时获取，现补发给你。祝你好运。"},
                    { "Filipino","Bayani, dahil sa aming pagkukulang, hindi namin nakuha agad ang binili mong produkto. Ngayon ay ipinapadala namin muli sa iyo. Magandang kapalaran sa iyo."},
                    { "Indonesian","Pahlawan, karena kami menemukan bahwa barang yang Anda beli tidak segera diperoleh, kami sekarang mengirimkannya kembali kepada Anda. Semoga beruntung untuk Anda."},
                    { "Malay","Hero, kerana kami dapati barang yang anda beli tidak diperolehi dengan cepat, kami kini menghantarnya semula kepada anda. Semoga berjaya untuk anda."},
                    { "Thai","วีรชนครับ/ค่ะ พบว่าสินค้าที่คุณซื้อไม่ได้รับไว้ทันที เราจึงจัดส่งใหม่ให้คุณครับ/ค่ะ ขอให้โชคดีครับ/ค่ะ"},
                };
                    commandMail.ToIds.Add(gc.Id);   //加入收件人
                    commandMail.Mail.Attachment.AddRange(forMail);     //加入附件
                    _SyncCommandManager.Handle(commandMail);
                }
                #endregion 发送奖品邮件
                _EntityManager.CreateAndMove(noMail, gc);
                //后处理
                jo.SendInMail = true;
                db.SaveChanges();
            }, orderId);
        }
        /*
*         lbErr:
   var list = new List<(GameEntitySummary, IEnumerable<GameEntitySummary>)> { };
   if (tt.ShoppingItem.Outs.Count > 0) //若有产出项
   {
       var b = _SpecialManager.Transformed(tt.ShoppingItem.Outs, list, new EntitySummaryConverterContext
       {
           Change = null,
           GameChar = gc,
           IgnoreGuarantees = false,
           Random = new Random(),
       });
       if (!b) goto lbErr;
   }
   var items = list.SelectMany(c => c.Item2);


* */
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

    /// <summary>
    /// 该渠道支付时，订单中的寄宿数据对象。用于满足特殊的"慢支付"流程需要。
    /// </summary>
    public class T0314JObject
    {
        /// <summary>
        /// 客户端获取订单的时间，若为null则未获取。
        /// </summary>
        public DateTime? NotifyDateTime { get; set; }

        /// <summary>
        /// 商品的TId。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 使用新方法处理发送货品。
        /// </summary>
        public bool IsClientCreate { get; set; }

        /// <summary>
        /// 是否通过Mail发送了奖品。null=未确定，true=已通过mail发送，false=已直接放入用户包裹。
        /// </summary>
        public bool? SendInMail { get; set; }

        /// <summary>
        /// 获取购买得到的物品摘要。
        /// </summary>
        public List<GameEntitySummary> EntitySummaries { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// 扩展字符串，通常放置 实际发放物品的变化数据。
        /// </summary>
        public string ExtraString { get; set; }
    }

    //public class T0314PayReturnStringDto
    //{
    //    public T0314PayReturnStringDto()
    //    {

    //    }

    //    /// <summary>
    //    /// 购买道具的用户uid。
    //    /// </summary>
    //    [JsonPropertyName("uid")]
    //    public string Uid { get; set; }

    //    /// <summary>
    //    /// 购买道具的用户username。
    //    /// </summary>
    //    [JsonPropertyName("username")]
    //    public string UserName { get; set; }

    //    /// <summary>
    //    /// 游戏下单时传递的游戏订单号，原样返回。
    //    /// </summary>
    //    [JsonPropertyName("cpOrderNo")]
    //    public string CpOrderNo { get; set; }

    //    /// <summary>
    //    /// SDK唯一订单号。
    //    /// </summary>
    //    [JsonPropertyName("orderNo")]
    //    public string OrderNo { get; set; }

    //    /// <summary>
    //    /// 用户支付时间，如2017-02-06 14:22:32
    //    /// </summary>
    //    [JsonPropertyName("payTime")]
    //    public string PayTime { get; set; }

    //    /// <summary>
    //    /// 订单支付方式，具体值对应支付渠道详见对照表.
    //    /// </summary>
    //    [JsonPropertyName("payType")]
    //    public string PayType { get; set; }

    //    /// <summary>
    //    /// 用户支付金额（单位：元）,注意：如果游戏商品有多个数量，那么金额就是单价*数量
    //    /// </summary>
    //    [JsonPropertyName("payAmount")]
    //    public string PayAmount { get; set; }

    //    /// <summary>
    //    /// 用户支付的币种，如RMB，USD等.
    //    /// </summary>
    //    [JsonPropertyName("payCurrency")]
    //    public string PayCurrency { get; set; }

    //    /// <summary>
    //    /// 用户支付的游戏道具以美元计价的金额（单位：元）.注意：如果游戏商品有多个数量，那么金额就是单价*数量
    //    /// </summary>
    //    [JsonPropertyName("usdAmount")]
    //    public string UsdAmount { get; set; }

    //    /// <summary>
    //    /// 支付状态，为0表示成功，为1时游戏不做处理
    //    /// </summary>
    //    [JsonPropertyName("payStatus")]
    //    public string PayStatus { get; set; }

    //    /// <summary>
    //    /// 充值折扣，取值范围0~1(不包含0），默认为1表示不折扣；如值为0.2表示多发20%的元宝
    //    /// </summary>
    //    [JsonPropertyName("actRate")]
    //    public string ActRate { get; set; }

    //    /// <summary>
    //    /// 游戏下单时传递的扩展参数，将原样返回。
    //    /// </summary>
    //    [JsonPropertyName("extrasParams")]
    //    public string ExtrasParams { get; set; }

    //    /// <summary>
    //    /// 内购订阅型商品订单使用，如果有此字段表示订单订阅状态。cp监测到有此字段时不需要发货。字段取值为：2：订阅取消
    //    /// </summary>
    //    [JsonPropertyName("subscriptionStatus")]
    //    public string SubscriptionStatus { get; set; }

    //    /// <summary>
    //    /// 内购订阅型商品订单取消订阅原因。当有subscriptionStatus字段时此字段必有
    //    /// </summary>
    //    [JsonPropertyName("subReason")]
    //    public string SubReason { get; set; }

    //    /// <summary>
    //    /// 签名值，游戏应根据签名约定，本地计算后与此值进行比对
    //    /// </summary>
    //    [JsonPropertyName("sign")]
    //    public string Sign { get; set; }

    //    public Dictionary<string, string> GetDic()
    //    {
    //        var dic = new Dictionary<string, string>();
    //        var coll = GetType().GetProperties().Select(c =>
    //        {
    //            var name = c.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? c.Name;
    //            return (name, c.GetValue(this)?.ToString());
    //        });
    //        return coll.ToDictionary(c => c.name, c => c.Item2);
    //    }
    //}

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
