using AutoMapper;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OW.Game;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.SyncCommand;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GY02.Controllers
{
    /// <summary>
    /// 商城功能控制器。
    /// </summary>
    public class ShoppingController : GameControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameAccountStore"></param>
        /// <param name="mapper"></param>
        /// <param name="syncCommandManager"></param>
        /// <param name="entityManager"></param>
        /// <param name="shoppingManager"></param>
        /// <param name="templateManager"></param>
        /// <param name="searcherManager"></param>
        public ShoppingController(GameAccountStoreManager gameAccountStore, IMapper mapper, SyncCommandManager syncCommandManager, GameEntityManager entityManager,
            GameShoppingManager shoppingManager, GameTemplateManager templateManager, GameSearcherManager searcherManager)
        {
            _GameAccountStore = gameAccountStore;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
            _EntityManager = entityManager;
            _ShoppingManager = shoppingManager;
            _TemplateManager = templateManager;
            _SearcherManager = searcherManager;
        }

        GameAccountStoreManager _GameAccountStore;
        IMapper _Mapper;
        SyncCommandManager _SyncCommandManager;
        GameEntityManager _EntityManager;
        GameShoppingManager _ShoppingManager;
        GameTemplateManager _TemplateManager;
        GameSearcherManager _SearcherManager;

#if DEBUG
        /// <summary>
        /// 获取商品项结构。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GameShoppingItem> GetShoppingItem()
        {
            return Ok();
        }

#endif
        /// <summary>
        /// 获取指定商品配置数据。
        /// </summary>
        /// <returns></returns>
        /// <response code="401">令牌无效。</response>  
        [HttpPost]
        public ActionResult<GetShoppingItemsReturnDto> GetShoppingItems(GetShoppingItemsParamsDto model)
        {
            var result = new GetShoppingItemsReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new GetShoppingItemsCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 按指定TId获取商品配置数据。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetShoppingItemsByIdsReturnDto> GetShoppingItemsByIds(GetShoppingItemsByIdsParamsDto model)
        {
            var result = new GetShoppingItemsByIdsReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            //获取基础集合
            IEnumerable<TemplateStringFullView> baseColl;

            baseColl = _TemplateManager.Id2FullView.Where(c => c.Value.ShoppingItem is not null && model.TIds.Contains(c.Value.TemplateId)).Select(c => c.Value);
            //刷新金猪周期
            if (_ShoppingManager.IsChanged(gc, "gs_jinzhu")) _ShoppingManager.JinzhuChanged(gc);
            //刷新礼包周期
            if (_ShoppingManager.IsChanged(gc, "gs_leijilibao")) _ShoppingManager.LibaoChanged(gc);
            //过滤
            DateTime nowUtc = OwHelper.WorldNow;    //当前
            List<(TemplateStringFullView, DateTime)> list = new List<(TemplateStringFullView, DateTime)>();
            foreach (var item in baseColl)  //遍历基础集合
            {
                var b = _ShoppingManager.IsMatchWithoutBuyed(gc, item, nowUtc, out var startUtc, 2);
                if (!b) continue;   //若不符合条件
                list.Add((item, startUtc));
            }
            var coll1 = list.Where(c => c.Item1.Genus.Contains("gs_meirishangdian")).ToArray();
            result.ShoppingItemStates.AddRange(list.Select(c =>
            {
                var tmp = new ShoppingItemStateDto
                {
                    TId = c.Item1.TemplateId,
                    StartUtc = c.Item2,
                    EndUtc = c.Item2 + c.Item1.ShoppingItem.Period.ValidPeriod,
                    BuyedCount = gc.ShoppingHistoryV2.Where(history => history.TId == c.Item1.TemplateId && history.WorldDateTime >= c.Item2 && history.WorldDateTime < c.Item2 + c.Item1.ShoppingItem.Period.ValidPeriod).Sum(c => c.Count),
                };
                var per = _SearcherManager.GetPeriodIndex(c.Item1.ShoppingItem.Ins, gc, out _);
                if (per.HasValue) //若有自周期
                {
                    var newBuyedCount = gc.ShoppingHistoryV2.Where(history => history.TId == c.Item1.TemplateId)
                        .Where(c => c.PeriodIndex == per).Sum(c => c.Count);
                    tmp.BuyedCount = newBuyedCount;
                }
                return tmp;
            }));

            return result;
        }

        /// <summary>
        /// 累计签到。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LeijiQiandaoReturnDto> LeijiQiandao(LeijiQiandaoParamsDto model)
        {
            var result = new LeijiQiandaoReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new LeijiQiandaoCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 购买指定商品。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ShoppingBuyReturnDto> ShoppingBuy(ShoppingBuyParamsDto model)
        {
            var result = new ShoppingBuyReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new ShoppingBuyCommand
            {
                GameChar = gc,
            };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            if (result.Changes?.Count > 0) result.HasError = false; //对多个购买物品时，买一次成功就算成功
            return result;
        }

        /// <summary>
        /// 购买指定商品，分开返回每一个商品引发的属性变化。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ShoppingBuyWithDetailReturnDto> ShoppingBuyWithDetail(ShoppingBuyWithDetailParamsDto model)
        {
            var result = new ShoppingBuyWithDetailReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            for (var i = 0; i < model.Count; i++)
            {
                var command = new ShoppingBuyCommand
                {
                    GameChar = gc,
                    ShoppingItemTId = model.ShoppingItemTId,
                    Count = 1,
                };
                _SyncCommandManager.Handle(command);
                if (command.HasError)
                {
                    result.FillErrorFrom(command);
                    return result;
                }
                var gamePropertyChanges = _Mapper.Map<List<GamePropertyChangeItemDto>>(command.Changes);
                result.Changes.Add(gamePropertyChanges);
            }
            //if (result.Changes?.Count > 0) result.HasError = false; //对多个购买物品时，买一次成功就算成功
            return result;
        }

        /// <summary>
        /// 客户端发起创建一个订单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CreateOrderReturnDto> CreateOrder(CreateOrderParamsDto model)
        {
            var result = new CreateOrderReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new CreateOrderCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 获取订单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="db">数据库访问上下文。</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetShoppingOrderReturnDto> GetShoppingOrder(GetShoppingOrderParamsDto model, [FromServices] GY02UserContext db)
        {
            var result = new GetShoppingOrderReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            var cid = gc.Id.ToString();
            var coll = db.Set<GameShoppingOrder>().Where(c => c.CreateUtc >= model.Start && c.CreateUtc <= model.End && c.CustomerId == cid).AsEnumerable();
            foreach (var c in coll)
            {
                var order = _Mapper.Map<GameShoppingOrderDto>(c);
                result.Orders.Add(order);
            }
            return result;
        }

        /// <summary>
        /// 执行兑换码兑换功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns>错误码是160表示指定的兑换码不存在。若错误码是1219则表示兑换码失效。</returns>
        [HttpPost]
        public ActionResult<RedeemReturnDto> Redeem(RedeemParamsDto model)
        {
            var result = new RedeemReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new RedeemCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 角色改名。
        /// </summary>
        /// <param name="model"></param>
        /// <returns>用户属性变化返回在Changes里。</returns>
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<RenameCharReturnDto> RenameChar(RenameCharParamsDto model)
        {
            var result = new RenameCharReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            List<GamePropertyChangeItem<object>> changes = new List<GamePropertyChangeItem<object>>();
            //购买的消耗商品
            //1d8542d3-da32-445e-9845-c9dc25e15cd5 修改名称商品购买
            var command = new ShoppingBuyCommand
            {
                GameChar = gc,
                Count = 1,
                ShoppingItemTId = Guid.Parse("1d8542d3-da32-445e-9845-c9dc25e15cd5"),
            };

            _SyncCommandManager.Handle(command);
            if (command.HasError)
            {
                result.FillErrorFrom(command);
                return result;
            }
            _Mapper.Map(command.Changes, result.Changes);
            //更新名字
            var ovDisplayName = gc.DisplayName;
            gc.DisplayName = model.DisplayName;
            changes.MarkChanges(gc, nameof(gc.DisplayName), ovDisplayName, gc.DisplayName);
            //更新更改次数
            var ov = gc.RenameCount;
            gc.RenameCount++;
            changes.MarkChanges(gc, nameof(gc.RenameCount), ov, gc.RenameCount);
            //记录变化数据
            var tmp = _Mapper.Map<List<GamePropertyChangeItemDto>>(changes);
            result.Changes.AddRange(tmp);
            _GameAccountStore.Save(gc.GetUser().Key);
            return result;
        }

        #region 法币购买相关

        /// <summary>
        /// 创建法币购买订单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CreateOrderV2ReturnDto> CreateOrderV2(CreateOrderV2ParamsDto model)
        {
            var result = new CreateOrderV2ReturnDto();
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            return result;
        }
        #endregion 法币购买相关

        #region T1021NA相关
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<T1021NAPayedReturnDto> T1021NAPayed(T1021NAPayedDto model)
        {
            var result = new T1021NAPayedReturnDto() { errcode = 1 };
            var nvs = StringUtility.Get(model);
            return result;
        }
        #endregion T1021NA相关
    }

    /// <summary>
    /// 创建法币购买订单功能的参数封装类。
    /// </summary>
    public class CreateOrderV2ParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 渠道标识，针对每个不同渠道会给出不同命名，通常是 渠道名/地区/平台，当然也可能不区分平台。
        /// </summary>
        public string Channel { get; set; }
    }

    /// <summary>
    /// 创建法币购买订单功能的返回值封装类。
    /// </summary>
    public class CreateOrderV2ReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// T1021合作伙伴北美支付回调参数封装类。
    /// </summary>
    public class T1021NAPayedDto
    {
        /// <summary>
        /// state 的状态 1 成功。
        /// </summary>
        [JsonPropertyName("state")]
        public int state { get; set; }

        /// <summary>
        /// 为Oasis的订单号。
        /// </summary>
        [JsonPropertyName("orderId")]
        public string? orderId { get; set; }

        /// <summary>
        /// 用户ID。
        /// </summary>
        [JsonPropertyName("userId")]
        public string? userId { get; set; }

        /// <summary>
        /// 为游戏编号，由Oasis 分配。
        /// </summary>
        [JsonPropertyName("uugameId")]
        public int uugameId { get; set; }

        /// <summary>
        /// 游戏区服编号。
        /// </summary>
        [JsonPropertyName("serverId")]
        public string? serverId { get; set; }

        /// <summary>
        /// 为渠道分配的渠道编号 iOS:2000100000 Google:2000100001 Huawei:2000100004 VIVO:2000100008 OPPO:2000100007 Web pay:2000100006 Web pay含游戏订单号：2000100005 
        /// Japan yamada : 2000100009 Japan Gesoten: 2000100010。
        /// </summary>
        [JsonPropertyName("channelId")]
        public string? channelId { get; set; }

        /// <summary>
        /// 游戏订单号我们原样返回，在Android中对应充值传递的gameCustomInfo，iOS中对应充值传递的code;针对网页充值包含游戏内网页直冲版本和游戏外充值网站版本版本，网站充值版本的channelId为：2000100006，
        /// 此时gameCustomInfo为游戏的角色ID；游戏内网页直冲版本的channelId为2000100005，此时gameCustomInfo为游戏透传的订单号。
        /// </summary>
        [JsonPropertyName("gameCustomInfo")]
        public string? gameCustomInfo { get; set; }

        /// <summary>
        /// 为玩家充值的商品 Id。
        /// </summary>
        [JsonPropertyName("productId")]
        public string? productId { get; set; }

        /// <summary>
        /// 为玩家充值的金额(美元金额)。
        /// </summary>
        [JsonPropertyName("amount")]
        public string? amount { get; set; }

        /// <summary>
        /// 为玩家充值的当地币金额(默认关闭)。
        /// </summary>
        [JsonPropertyName("payAmount")]
        public string? payAmount { get; set; }

        /// <summary>
        /// 玩家充值的当地币币种(默认关闭)。
        /// </summary>
        [JsonPropertyName("payCurrency")]
        public string? payCurrency { get; set; }

        /// <summary>
        /// 玩家点击的商品充值获得钻石数(该参数目前只针对网站充值)。
        /// </summary>
        [JsonPropertyName("diamondAmount")]
        public int diamondAmount { get; set; }

        /// <summary>
        /// 用户多充值的钻石数(该参数目前只针对网站充值)。
        /// </summary>
        [JsonPropertyName("diamondExtraAmount")]
        public int diamondExtraAmount { get; set; }

        /// <summary>
        /// 表示实际充值金额的总返利(该参数目前只针对网站充值)。
        /// </summary>
        [JsonPropertyName("diamondGiftExtraAmount")]
        public int diamondGiftExtraAmount { get; set; }

        /// <summary>
        /// 对上边参数的签名数据，注：sign不参与签名。
        /// </summary>
        [JsonPropertyName("sign")]
        public string? sign { get; set; }

        /// <summary>
        /// 签名方式，固定值“RSA”，对于java中的“SHA1WithRSA” 注：signtype不参与签名。
        /// </summary>
        [JsonPropertyName("signtype")]
        public string? signtype { get; set; }


    }

    /// <summary>
    /// T1021合作伙伴北美支付回调返回值封装类。
    /// </summary>
    public class T1021NAPayedReturnDto
    {
        /// <summary>
        /// 1=成功，101=订单重复，-100=其他错误。
        /// </summary>
        [JsonPropertyName("errcode")]
        public int errcode { get; set; }

        /// <summary>
        /// 错误信息。
        /// </summary>
        [JsonPropertyName("errMsg")]
        public string? errMsg { get; set; }
    }

    /// <summary>
    /// 常用工具。
    /// </summary>
    public static class StringUtility
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IEnumerable<(string, string)> Get<T>(T data)
        {
            var result = new List<(string, string)>();
            var pis = typeof(T).GetProperties();
            string name;
            foreach (var pi in pis)
            {
                if (pi.GetCustomAttribute<JsonPropertyNameAttribute>() is JsonPropertyNameAttribute jpna) name = jpna.Name;
                else name = pi.Name;
                if(pi.PropertyType == typeof(string))
                {

                }
                //else if(typeof(DBNull<>)==){ }
                var val = pi.GetValue(data)!.ToString()!;
                result.Add((name, val));
            }
            return result;
        }
    }
}
