using AutoMapper;
using GY02;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OW.Game.Managers;
using OW.Game.Store;
using OW.SyncCommand;
using System.Security.Cryptography;
using System.Text.Json;

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
        public T304Controller(ILogger<T304Controller> logger, GameAccountStoreManager accountStoreManager, GameTemplateManager templateManager, GY02UserContext dbContext, IMapper mapper, SyncCommandManager syncCommandManager)
        {
            _Logger = logger;
            _AccountStoreManager = accountStoreManager;
            _TemplateManager = templateManager;
            _DbContext = dbContext;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
            //完美 北美
        }

        /// <summary>
        /// 日志接口。
        /// </summary>
        ILogger<T304Controller> _Logger;
        GameAccountStoreManager _AccountStoreManager;
        GameTemplateManager _TemplateManager;
        GY02UserContext _DbContext;
        IMapper _Mapper;
        SyncCommandManager _SyncCommandManager;

        /// <summary>
        /// 付款结束确认。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<T304PayedReturnDto> Payed(T304PayedParamsDto model)
        {
            _Logger.LogInformation($"T304/Payed收到支付确认调用，参数：{JsonSerializer.Serialize(model)}");
            var result = new T304PayedReturnDto();
            using var dw = _AccountStoreManager.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var tt = _TemplateManager.Id2FullView.Values.FirstOrDefault(c => c.ProductStoreId == model.ProductId);
            if (tt is null)
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = $"找不到指定商品——{model.ProductId}";
                _Logger.LogWarning(result.DebugMessage);
                //result.DebugMessage = result.DebugMessage;
                return result;

            }
            var id = Guid.NewGuid();
            _Logger.LogInformation($"T304/Payed确认支付调用。id={id}");
            result.DebugMessage = $"{id}";
            //透参的格式是：角色Id,商品TId,如:2A92C88B-CF43-4C47-9B53-0832CCCBD805,D330809B-AF3C-4A71-B2ED-9D9AABED4281
            //string payload = "";    //透参
            //var ary = payload.Split(',');
            //if (ary.Length != 2 || !Guid.TryParse(ary[0], out var gcId/*角色Id*/) || !Guid.TryParse(ary[1], out var shoppingTid/*商品Id*/))
            //{
            //    result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
            //    result.DebugMessage = $"透参格式错误——{payload}";
            //    _Logger.LogWarning(result.DebugMessage);
            //    //result.DebugMessage = result.DebugMessage;
            //    return result;
            //}
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
                //Amount = model.Amount,
                //Currency = model.Currency,
                Confirm1 = true,
                Confirm2 = true,
                CustomerId = gc?.Id.ToString(),
                CompletionDateTime = OwHelper.WorldNow,
                Amount = tt.Amount,
                Currency = tt.CurrencyCode,
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
                return result;
            }
            _Logger.LogInformation("订单号{id}已经确认成功。", order.Id);

            return result;
        }

    }

    //    public class GooglePayDto {
    //string packageName;
    //String productId;
    //string purchaseToken;
    //public string getPackageName() (return packageName:
    //public void setPackageName(string packageName) [this.packageName = packageName;
    //public string getProductId() [return productId;
    //public void setProductId(string productId)
    //    {
    //        this.productId = productid;
    //        public string getPurchaseToken()
    //        {
    //            return purchaseToken;
    //            public void setPurchaseToken(string purchaseToken) this.purchaseToken = purchaseToken;
    //        }
    //    }
}