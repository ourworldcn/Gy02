using AutoMapper;
using GY02;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.SyncCommand;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gy02.Controllers
{
    /// <summary>
    /// T304合作伙伴功能控制器。
    /// </summary>
    public class T304Controller : GameControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public T304Controller(ILogger<T304Controller> logger, GameAccountStoreManager accountStoreManager, GameTemplateManager templateManager, GY02UserContext dbContext, IMapper mapper, SyncCommandManager syncCommandManager, T304Manager t304Manager, SpecialManager specialManager)
        {
            //完美 北美
            _Logger = logger;
            _AccountStore = accountStoreManager;
            _TemplateManager = templateManager;
            _DbContext = dbContext;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
            _T304Manager = t304Manager;
            _SpecialManager = specialManager;
        }

        /// <summary>
        /// 日志接口。
        /// </summary>
        ILogger<T304Controller> _Logger;
        GameAccountStoreManager _AccountStore;
        GameTemplateManager _TemplateManager;
        GY02UserContext _DbContext;
        IMapper _Mapper;
        SyncCommandManager _SyncCommandManager;
        T304Manager _T304Manager;
        SpecialManager _SpecialManager;

        /// <summary>
        /// 付款结束确认。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<T304PayedReturnDto> Payed(T304PayedParamsDto model)
        {
            _Logger.LogInformation("T304/Payed收到支付确认调用，参数：{str}", JsonSerializer.Serialize(model));
            var result = new T304PayedReturnDto();
            using var dw = _AccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            if (string.IsNullOrWhiteSpace(model.Data))
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = $"签名数据不可为空。";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            string orderId;
            try
            {
                var jo = JsonSerializer.Deserialize<T304Data>(model.Data)!;
                orderId = jo.OrderId;
            }
            catch (Exception)
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = $"签名数据格式不正确。";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            if (!_T304Manager.Verify(model.Data, model.Sign))
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = $"签名无效。";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            var orderIdBinary = Encoding.UTF8.GetBytes(orderId);
            var orderOld = _DbContext.ShoppingOrder.FirstOrDefault(c => c.BinaryArray == orderIdBinary);
            if (orderOld is not null)
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = $"订单已被入库，不可重复入库。";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            var tt = _TemplateManager.Id2FullView.Values.FirstOrDefault(c =>
            {
                var r = c.ProductInfo?.Values.FirstOrDefault(d => d.TryGetValue("productStoreId", out var s) && string.Equals(model.ProductId, s, StringComparison.OrdinalIgnoreCase));
                return r != null;
                //return c.ProductStoreId == model.ProductId;
            });
            if (tt is null)
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = $"找不到指定商品——{model.ProductId}";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            var dicProductInfo = tt.ProductInfo.Values.First(d => d.TryGetValue("productStoreId", out var s) && string.Equals(model.ProductId, s, StringComparison.OrdinalIgnoreCase))!;
            var id = Guid.NewGuid();
            _Logger.LogInformation($"T304/Payed确认支付调用。id={id}");
            result.DebugMessage = $"{id}";

            #region 内部购买

            var bi = gc!.HuoBiSlot.Children.FirstOrDefault(c => c.TemplateId == ProjectContent.FabiTId);  //法币占位符
            if (bi is null)
            {
                result.DebugMessage = $"法币占位符为空。CharId={gc.Id}";
                result.ErrorCode = ErrorCodes.ERROR_INVALID_DATA;
                result.HasError = true;
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            bi.Count++;
            var command = new ShoppingBuyCommand
            {
                Count = 1,
                GameChar = gc,
                ShoppingItemTId = tt.TemplateId,
            };
            _SyncCommandManager.Handle(command);
            if (command.HasError)
            {
                if (bi.Count > 0) bi.Count--;
                result.FillErrorFrom(command);
                _Logger.LogWarning("出现错误——{msg}", result.DebugMessage);
                return result;
            }
            _Mapper.Map(command.Changes, result.Changes);
            #endregion 内部购买

            var order = new GameShoppingOrder
            {
                Confirm1 = true,
                Confirm2 = true,
                CustomerId = gc?.Id.ToString(),
                CompletionDateTime = OwHelper.WorldNow,
                Amount = tt.Amount,
                Currency = tt.CurrencyCode,
                BinaryArray = orderIdBinary,
            };
            _DbContext.ShoppingOrder.Add(order);
            try
            {
                _DbContext.SaveChanges();
            }
            catch (Exception err)
            {
                _Logger.LogWarning("保存订单号{id}的订单时出错——{msg}", order.Id, err.Message);
                result.DebugMessage = $"保存订单号{order.Id}的订单时出错——{err.Message}";
                result.ErrorCode = ErrorCodes.ERROR_INVALID_DATA;
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            _Logger.LogInformation("订单号{id}已经确认成功。", order.Id);

            return result;
        }

        /// <summary>
        /// 储值回调。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<T304PayedV2ReturnDto> T304PayedV2([FromForm] T304PayedV2ParamsDto model)
        {
            // https://m60bysk56u.feishu.cn/docx/ZWPkd11xso08ZSxYyDTcr10dnoc
            var result = new T304PayedV2ReturnDto();
            _Logger.LogInformation("T304/T304PayedV2收到支付确认调用，参数：{str}", JsonSerializer.Serialize(model));
            string? errMsg = null;
            if (!Guid.TryParse(model.AppExtraInfo, out var orderId))
            {
                errMsg = $"无效的透传参数:{model.AppExtraInfo}";
                goto lbErr;
            }
            if (_DbContext.ShoppingOrder.Find(orderId) is not GameShoppingOrder order)
            {
                errMsg = $"找不到指定的订单Id:Id={orderId}";
                goto lbErr;
            }
            var keyGu = _AccountStore.GetKeyByCharId(Guid.Parse(order.CustomerId));
            using (var dw = _AccountStore.GetOrLoadUser(keyGu, out var gu))
            {
                if (dw.IsEmpty)
                {
                    errMsg = $"无法找到角色:GameCharId={order.CustomerId}";
                    goto lbErr;
                }
                if (order.Confirm2)  //若订单已经完成
                    return result;
                else order.Confirm2 = true;
                var jo = order.GetJsonObject<T304PayedV2JObject>();
                order.Amount = model.MoneyAmount;
                order.Currency = model.MoneyCurrency;
                if (order.Confirm1) //若需要发货
                {
                    order.State = 1;
                    var gc = gu.CurrentChar;
                    var command = new ShoppingBuyCommand
                    {
                        Count = 1,
                        GameChar = gu.CurrentChar,
                        ShoppingItemTId = jo.TId,
                    };
                    _SyncCommandManager.Handle(command);
                    if (command.HasError)
                    {
                        errMsg = command.DebugMessage;
                        goto lbErr;
                    }
                    List<GamePropertyChangeItemDto> changes = new List<GamePropertyChangeItemDto>();
                    _Mapper.Map(command.Changes, changes);
                    jo.ExtraString = JsonSerializer.Serialize(changes);
                }
                if (order.Confirm1 && order.Confirm2) order.State = 1;
            }
            _DbContext.SaveChanges();
            return result;
        lbErr:
            _Logger.LogWarning(errMsg);
            result.Code = 1;
            return result;
        }

        /// <summary>
        /// 创建 T304V2 订单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">无效令牌。</response> 
        [HttpPost]
        public ActionResult<CreateT304V2OrderReturnDto> CreateT304V2Order(CreateT304V2OrderParamsDto model)
        {
            var result = new CreateT304V2OrderReturnDto { };
            using var dw = _AccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            GameShoppingOrder order = new GameShoppingOrder()
            {
                Confirm1 = true,
                CustomerId = gc.Id.ToString(),
            };

            #region 初始化数据对象信息
            var jo = order.GetJsonObject<T304PayedV2JObject>();
            jo.TId = model.ShoppingItemTId;

            if (_TemplateManager.GetFullViewFromId(model.ShoppingItemTId) is not TemplateStringFullView tt)
            {
                result.FillErrorFromWorld();
                return result;
            }
            var list = new List<(GameEntitySummary, IEnumerable<GameEntitySummary>)> { };
            if (!_SpecialManager.Transformed(tt, list, gc))
            {
                result.FillErrorFromWorld();
                return result;
            }
            jo.EntitySummaries.AddRange(list.SelectMany(c => c.Item2));
            #endregion 初始化数据对象信息

            _DbContext.Add(order);
            _DbContext.SaveChanges();
            result.OrderId = order.Id;

            return result;
        }

        /// <summary>
        /// 查询T304V2 订单。最多6秒返回。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">无效令牌。</response> 
        [HttpGet]
        public ActionResult<GetT304V2OrderReturnDto> GetT304V2Order([FromQuery] GetT304V2OrderParamsDto model)
        {
            var result = new GetT304V2OrderReturnDto();
            using var dw = _AccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            var now = OwHelper.WorldNow;
            var endDt = now + TimeSpan.FromSeconds(6);
            GameShoppingOrder? order = null;
            for (var tmp = now; tmp <= endDt; tmp = OwHelper.WorldNow)
            {
                order = _DbContext.ShoppingOrder.FirstOrDefault(c => c.Id == model.OrderId);
                if (order is null)
                {
                    //result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    //result.DebugMessage = $"无此订单，{nameof(model.OrderId)}={model.OrderId}";
                    //result.HasError = true;
                    Thread.Sleep(500);
                    continue;
                }
                if (order.State == 0)
                {
                    Thread.Sleep(500);
                    continue;
                }
                if (Guid.TryParse(order.CustomerId, out var gcId))
                    if (gcId != gc.Id)    //若不是自己的订单
                    {
                        result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                        result.DebugMessage = $"只能查询自己的订单。";
                        result.HasError = true;
                        return result;
                    }
                result.Order = _Mapper.Map<GameShoppingOrderDto>(order);
                var jo = order.GetJsonObject<T304PayedV2JObject>();
                result.Changes.AddRange(string.IsNullOrWhiteSpace(jo.ExtraString) ? new List<GamePropertyChangeItemDto>() : JsonSerializer.Deserialize<List<GamePropertyChangeItemDto>>(jo.ExtraString)!);
                result.Order.Changes.AddRange(string.IsNullOrWhiteSpace(jo.ExtraString) ? new List<GamePropertyChangeItemDto>() : JsonSerializer.Deserialize<List<GamePropertyChangeItemDto>>(jo.ExtraString)!);
            }
            return result;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class T304Data
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public T304Data()
        {

        }

        /// <summary>
        /// 订单Id。
        /// </summary>
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = null!;

        /// <summary>
        /// 平台的商品Id。
        /// </summary>
        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = null!;
    }

    /// <summary>
    /// 304支付接口V2订单的额外数据对象。
    /// </summary>
    public class T304PayedV2JObject
    {
        /// <summary>
        /// 商品Id。
        /// </summary>
        public Guid TId { get; set; }

        /// <summary>
        /// 获取购买得到的物品摘要。
        /// </summary>
        public List<GameEntitySummary> EntitySummaries { get; set; } = new List<GameEntitySummary>();

        /// <summary>
        /// 扩展字符串，通常放置 实际发放物品的变化数据。
        /// </summary>
        public string ExtraString { get; set; }
    }
}