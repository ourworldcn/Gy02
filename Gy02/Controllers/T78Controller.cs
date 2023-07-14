using GY02;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gy02.Controllers
{
    /// <summary>
    /// T78合作伙伴调入控制器。
    /// </summary>
    public class T78Controller : GameControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="t78Manager"></param>
        /// <param name="gameAccountStore"></param>
        /// <param name="shoppingManager"></param>
        public T78Controller(PublisherT78Manager t78Manager, GameShoppingManager shoppingManager, GameAccountStoreManager gameAccountStore, GameEntityManager entityManager)
        {
            _T78Manager = t78Manager;
            _ShoppingManager = shoppingManager;
            _GameAccountStore = gameAccountStore;
            _EntityManager = entityManager;
        }

        PublisherT78Manager _T78Manager;
        GameShoppingManager _ShoppingManager;
        GameAccountStoreManager _GameAccountStore;
        GameEntityManager _EntityManager;

        /// <summary>
        /// T78合作伙伴充值回调。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="isSandbox">"1"表示沙箱；其他表示正式</param>
        /// <param name="payType">"mycard"表示mycard;"google"表示google-play支付;"mol"表示mol支付;"apple"表示苹果支付;“onestore”韩国onestore商店支付;“samsung”三星支付
        /// </param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<PayedReturnDto> Payed([FromForm] PayedParamsDto model, [FromHeader(Name = "X-BNPAY-SANDBOX")] string isSandbox, [FromHeader(Name = "X-BNPAY-PAYTYPE")] string payType)
        {
            var result = new PayedReturnDto();
            var dic = model.GetDictionary();
            var sign = _T78Manager.GetSignature(dic);
            if (sign != model.Sign) //若签名不正确
            {
                result.Result = 1;
                result.DebugMessage = $"签名不正确。";
                return result;
            }
            var str = _ShoppingManager.DecodeString(model.CallbackInfo);
            if (!Guid.TryParse(str, out var orderId))   //若无法获取订单Id。
            {
                result.Result = 1;
                result.DebugMessage = $"透传参数错误。";
                return result;
            }
            var db = HttpContext.RequestServices.GetRequiredService<GY02UserContext>();
            var gcIdString = db.ShoppingOrder.Where(c => c.Id == orderId).Select(c => c.CustomerId).FirstOrDefault();
            if (!Guid.TryParse(gcIdString, out var gcId))   //若无法获取角色Id。
            {
                result.Result = 1;
                result.DebugMessage = $"若无法获取角色Id。";
                return result;
            }
            using var dw = _GameAccountStore.LockByCharId(gcId, db);
            if (dw.IsEmpty) //若无法锁定key
            {
                result.Result = 1;
                result.DebugMessage = OwHelper.GetLastErrorMessage();
                return result;
            }
            GameShoppingOrder order = db.ShoppingOrder.Find(orderId)!;  //获取订单
            order.JsonObjectString = JsonSerializer.Serialize(model);   //记录入参数
            if (model.OrderStatus == "0")  //若支付失败
            {
                order.State = 3;
                goto lbReturn;
            }
            if (model.ProductId != order.Detailes[0].GoodsId)   //若商品Id不一致
            {
                order.State = 2;
                result.Result = 1;
                result.DebugMessage = $"商品Id不一致。";
                goto lbReturn;
            }
            if (!decimal.TryParse(model.Money, out var money))    //若无法获取金额
            {
                result.Result = 1;
                result.DebugMessage = $"若无法锁定key。";
                goto lbReturn;
            }
            order.Amount = money;
            order.Currency = model.Currency;

            order.Confirm2 = true;  //确定
            order.State = 1;

            if (order.Detailes.Count > 0)    //若需要计算产出
            {
                var changes = new List<GamePropertyChangeItem<object>>();
                var coll = order.Detailes.Select(c => new GameEntitySummary(Guid.Parse(c.GoodsId), c.Count));
                if (!_EntityManager.CreateAndMove(coll, new OW.Game.Entity.GameChar { }, changes))
                {
                    result.Result = 1;
                    result.DebugMessage = OwHelper.GetLastErrorMessage();
                    goto lbReturn;
                }
            }
        lbReturn:
            db.SaveChanges();
            return result;
        }

        /// <summary>
        /// 客户端在T78合作伙伴上调用了充值信息后调用此接口通知服务器。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ClientPayedReturnDto> ClientPayed(ClientPayedParamsDto model)
        {
            ClientPayedReturnDto result = new ClientPayedReturnDto { };
            return result;
        }
    }

}
