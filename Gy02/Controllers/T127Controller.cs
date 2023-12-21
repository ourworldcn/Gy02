using AutoMapper;
using GY02;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Managers;
using OW.Game.Store;
using OW.SyncCommand;
using OW.TemplateDb.Entity;
using System.Text.Json;

namespace Gy02.Controllers
{
    /// <summary>
    /// 127伙伴接入相关。
    /// </summary>
    public class T127Controller : GameControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public T127Controller(T127Manager t127Manager, IHttpClientFactory httpClientFactory, ILogger<T127Controller> logger, GameShoppingManager shoppingManager,
            GameTemplateManager templateManager, GameAccountStoreManager accountStoreManager, GY02UserContext dbContext, SyncCommandManager syncCommandManager, IMapper mapper)
        {
            _T127Manager = t127Manager;
            _HttpClientFactory = httpClientFactory;
            _Logger = logger;
            _ShoppingManager = shoppingManager;
            _TemplateManager = templateManager;
            _AccountStoreManager = accountStoreManager;
            _DbContext = dbContext;
            _SyncCommandManager = syncCommandManager;
            _Mapper = mapper;
        }

        T127Manager _T127Manager;
        IHttpClientFactory _HttpClientFactory;
        ILogger<T127Controller> _Logger;
        GameShoppingManager _ShoppingManager;
        GameTemplateManager _TemplateManager;
        GameAccountStoreManager _AccountStoreManager;
        GY02UserContext _DbContext;
        SyncCommandManager _SyncCommandManager;
        IMapper _Mapper;

        /// <summary>
        /// 通知服务器完成T127的订单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CompleteOrderReturnDto> CompleteOrder(CompleteOrderParamsDto model)
        {
            var result = new CompleteOrderReturnDto();
            using var dw = _AccountStoreManager.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            using var client = _HttpClientFactory.CreateClient();
            _Logger.LogInformation("收到T127购买确认，参数 :ProductId= {ProductId},X-Token= {Token}，AccountId = {AccountId},Amount= {Amount},Currency= {Currency}",
                model.ProductId, model.PurchaseToken, model.OrderId, model.Amount, model.Currency);

            var tt = _TemplateManager.Id2FullView.FirstOrDefault(c => c.Value.ShoppingItem is not null && c.Value.ProductStoreId == model.ProductId).Value;
            if (tt is null)    //找不到指定的ProductId对应的商品
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = "找不到指定的ProductId对应的商品。";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }

            var b = _T127Manager.GetOrderState(model.ProductId, model.PurchaseToken, out var returnData);
            if (!b) //若无法验证订单
            {
                result.FillErrorFromWorld();
                return result;
            }

            if (/*returnData.purchaseState != 0 ||*/ returnData.consumptionState != 1)    //若尚未付款成功
            {
                result.DebugMessage = $"未付款成功。";
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            if (returnData.orderId != model.OrderId)   //若无法对应透传参数
            {
                result.DebugMessage = $"透传参数ObfuscatedExternalAccountId错误。";
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }

            var order = new GameShoppingOrder
            {
                Amount = model.Amount,
                Currency = model.Currency,
                Confirm1 = true,
                Confirm2 = true,
                CustomerId = gc?.Id.ToString(),
            };

            _DbContext.ShoppingOrder.Add(order);
            _Logger.LogDebug("开始计算产出。订单号={orderId}", order.Id);

            var bi = gc!.HuoBiSlot.Children.FirstOrDefault(c => c.TemplateId == ProjectContent.FabiTId);  //法币占位符
            if (bi is null)
            {
                result.DebugMessage = $"法币占位符为空。CharId={gc.Id}";
                result.ErrorCode = ErrorCodes.ERROR_INVALID_DATA;
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
                return result;
            }
            order.State = 1;
            _Mapper.Map(command.Changes, result.Changes);
            _Logger.LogDebug("订单号{id}已经确认成功。", order.Id);

            try
            {
                _DbContext.SaveChanges();
            }
            catch (Exception err)
            {
                _Logger.LogWarning("保存订单号{id}的订单时出错——{msg}", order.Id, err.Message);
            }
            return result;
        }
    }

}
