﻿using AutoMapper;
using GY02;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OW.Game.Entity;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GY02.Controllers
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
        /// <param name="entityManager"></param>
        /// <param name="logger"></param>
        public T78Controller(PublisherT78Manager t78Manager, GameShoppingManager shoppingManager, GameAccountStoreManager gameAccountStore, GameEntityManager entityManager, ILogger<T78Controller> logger)
        {
            _T78Manager = t78Manager;
            _ShoppingManager = shoppingManager;
            _GameAccountStore = gameAccountStore;
            _EntityManager = entityManager;
            _Logger = logger;
        }

        PublisherT78Manager _T78Manager;
        GameShoppingManager _ShoppingManager;
        GameAccountStoreManager _GameAccountStore;
        GameEntityManager _EntityManager;
        ILogger<T78Controller> _Logger;

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
            //using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            //{
            //    var str1 = reader.ReadToEndAsync().Result;
            //    _Logger.LogInformation("收到T78充值回调，参数 = {str1}", str1);
            //}

            _Logger.LogInformation("收到T78充值回调，参数 = {model},X-BNPAY-SANDBOX = {isSandbox}，X-BNPAY-PAYTYPE = {payType}", JsonSerializer.Serialize(model), isSandbox, payType);
            var result = new PayedReturnDto { };
            var dic = model.GetDictionary();
            var sign = _T78Manager.GetSignature(dic);
            if (sign != model.Sign) //若签名不正确
            {
                result.Result = 1;
                result.DebugMessage = $"签名不正确。";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }

            var str = _ShoppingManager.DecodeString(model.CallbackInfo ?? string.Empty);
            if (!Guid.TryParse(str, out var orderId))   //若无法获取订单Id。
            {
                result.Result = 1;
                result.DebugMessage = $"透传参数错误。";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            var db = HttpContext.RequestServices.GetRequiredService<GY02UserContext>();
            var gcIdString = db.ShoppingOrder.Where(c => c.Id == orderId).Select(c => c.CustomerId).FirstOrDefault();
            if (!Guid.TryParse(gcIdString, out var gcId))   //若无法获取角色Id。
            {
                result.Result = 1;
                result.DebugMessage = $"无法获取角色Id。{gcIdString}";
                _Logger.LogWarning(result.DebugMessage);
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
            if (model.OrderStatus != "1")  //若支付失败
            {
                order.State = 3;
                goto lbReturn;
            }
            if (model.Product_Id != order.Detailes[0].GoodsId)   //若商品Id不一致
            {
                order.State = 2;
                result.Result = 1;
                result.DebugMessage = $"商品Id不一致。";
                _Logger.LogWarning(result.DebugMessage);
                goto lbReturn;
            }
            if (!decimal.TryParse(model.Money, out var money))    //若无法获取金额
            {
                result.Result = 1;
                result.DebugMessage = $"若无法锁定key。";
                _Logger.LogWarning(result.DebugMessage);
                goto lbReturn;
            }
            order.Amount = money;
            order.Currency = model.Currency;

            if (order.Detailes.Count > 0)    //若需要计算产出
            {
                var key = dw.State as string;
                var id = Guid.Parse(key!);
                using var dwKey = _GameAccountStore.GetOrLoadUser(dw.State as string, out var gu);
                if (dwKey.IsEmpty)
                {
                    result.Result = 1;
                    result.DebugMessage = $"无法找到指定用户。key={dw.State as string}";
                    _Logger.LogWarning(result.DebugMessage);
                    goto lbReturn;
                }

                var changes = new List<GamePropertyChangeItem<object>>();
                List<(List<GameEntitySummary>, int)> ges = new List<(List<GameEntitySummary>, int)>();  //物品要买的物品集合
                foreach (var item in order.Detailes)
                {
                    if (!Guid.TryParse(item.GoodsId, out var tid)) goto lbErr;
                    var tt = _ShoppingManager.GetShoppingItemByTId(tid);
                    if (tt is null) goto lbErr;
                    ges.Add((tt.Outs, (int)item.Count));
                }
                List<(GameEntitySummary, GameEntity)> entities = new List<(GameEntitySummary, GameEntity)>();
                foreach (var item in ges)
                {
                    for (int i = 0; i < item.Item2; i++)
                    {
                        if (!_EntityManager.CreateAndMove(item.Item1, gu.CurrentChar, changes))
                        {
                            result.Result = 1;
                            result.DebugMessage = OwHelper.GetLastErrorMessage();
                            goto lbReturn;
                        }
                    }
                }
                var mapper = HttpContext.RequestServices.GetRequiredService<IMapper>();
                var tmp = changes.Select(c => mapper.Map<GamePropertyChangeItemDto>(c));
                var str1 = JsonSerializer.Serialize(tmp);
                order.BinaryArray = Encoding.UTF8.GetBytes(str1);  //设置变化数据
                order.Confirm2 = true;
                order.State = 1;
            }
            _Logger.LogDebug("订单号{id}已经确认成功。", order.Id);
        lbReturn:
            try
            {
                db.SaveChanges();
            }
            catch (Exception err)
            {
                _Logger.LogWarning("保存订单号{id}的订单时出错——{msg}", order.Id, err.Message);
            }
            return result;
        lbErr:
            result.FillErrorFromWorld();
            result.Result = 1;
            return result;
        }

        /// <summary>
        /// 客户端在T78合作伙伴退款通知回调函数。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<T78RefundReturnDto> Refund([FromForm] T78RefundParamsDto model)
        {
            _Logger.LogInformation("收到T78退款回调，参数 = {model}", JsonSerializer.Serialize(model));
            T78RefundReturnDto result = new T78RefundReturnDto { };

            var dic = model.GetDictionary();
            var sign = _T78Manager.GetSignature(dic);
            if (sign != model.Sign) //若签名不正确
            {
                result.Result = 1;
                result.DebugMessage = $"签名不正确。";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            if (!Guid.TryParse(model.CpOrderSn, out var orderId))  //若订单号无效
            {
                result.Result = 1;
                result.DebugMessage = $"无效的研发订单号,CpOrderSn={model.CpOrderSn}";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            var db = HttpContext.RequestServices.GetRequiredService<GY02UserContext>();
            var order = db.ShoppingOrder.Where(c => c.Id == orderId).FirstOrDefault();
            if (order is null)
            {
                result.Result = 1;
                result.DebugMessage = $"无法找到指定订单,OrderId={orderId}";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            var newOrder = new GameShoppingOrder
            {
                JsonObject = JsonSerializer.Serialize(model),
                Confirm1 = true,
                Confirm2 = true,
                State = 1,
                CustomerId = order.CustomerId,
                Currency = order.Currency,
                Amount = -order.Amount
            };
            try
            {
                db.Add(newOrder);
                db.SaveChanges();
            }
            catch (Exception)
            {
                result.Result = 1;
                result.DebugMessage = $"无法保存冲红订单,Order={JsonSerializer.Serialize(newOrder)}";
                _Logger.LogWarning(result.DebugMessage);
            }
            return result;
        }
    }

}
