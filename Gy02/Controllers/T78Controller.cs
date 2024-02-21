using AutoMapper;
using GY02;
using GY02.Base;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.GameDb;
using OW.SyncCommand;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
        /// <param name="syncCommandManager"></param>
        /// <param name="specialManager"></param>
        /// <param name="blueprintManager"></param>
        /// <param name="searcherManager"></param>
        public T78Controller(PublisherT78Manager t78Manager, GameShoppingManager shoppingManager, GameAccountStoreManager gameAccountStore,
            GameEntityManager entityManager, ILogger<T78Controller> logger, SyncCommandManager syncCommandManager, SpecialManager specialManager,
            GameBlueprintManager blueprintManager, GameSearcherManager searcherManager, GameSqlLoggingManager sqlLoggingManager)
        {
            _T78Manager = t78Manager;
            _ShoppingManager = shoppingManager;
            _GameAccountStore = gameAccountStore;
            _EntityManager = entityManager;
            _Logger = logger;
            _SyncCommandManager = syncCommandManager;
            _SpecialManager = specialManager;
            _BlueprintManager = blueprintManager;
            _SearcherManager = searcherManager;
            _SqlLoggingManager = sqlLoggingManager;
        }

        PublisherT78Manager _T78Manager;
        GameShoppingManager _ShoppingManager;
        GameAccountStoreManager _GameAccountStore;
        GameEntityManager _EntityManager;
        ILogger<T78Controller> _Logger;
        SyncCommandManager _SyncCommandManager;
        SpecialManager _SpecialManager;
        GameBlueprintManager _BlueprintManager;
        GameSearcherManager _SearcherManager;
        GameSqlLoggingManager _SqlLoggingManager;

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

            _Logger.LogInformation("收到T78充值回调，参数 : {model},X-BNPAY-SANDBOX = {isSandbox}，X-BNPAY-PAYTYPE = {payType}", JsonSerializer.Serialize(model), isSandbox, payType);
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
                result.DebugMessage = $"若无法获取交易金额。";
                _Logger.LogWarning(result.DebugMessage);
                goto lbReturn;
            }
            order.Amount = money;
            order.Currency = model.Currency;

            _Logger.LogDebug("开始计算产出。订单号={orderId}", orderId);

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
                var gc = gu.CurrentChar;
                var bi = gc.HuoBiSlot.Children.FirstOrDefault(c => c.TemplateId == ProjectContent.FabiTId);  //法币占位符
                if (bi is null)
                {
                    result.Result = 1;
                    result.DebugMessage = $"法币占位符为空。CharId={gc.Id}";
                    _Logger.LogWarning(result.DebugMessage);
                    goto lbReturn;
                }

                var mapper = HttpContext.RequestServices.GetRequiredService<IMapper>();
                var changes = new List<GamePropertyChangeItemDto> { };
                //购买
                foreach (var item in order.Detailes)
                {
                    for (int i = 0; i < item.Count; i++)
                    {
                        var command = new ShoppingBuyCommand()
                        {
                            GameChar = gc,
                            ShoppingItemTId = Guid.Parse(item.GoodsId),
                            Count = 1,
                        };
                        bi.Count++;
                        _SyncCommandManager.Handle(command);
                        if (command.HasError)
                        {
                            result.FillErrorFrom(command);
                            goto lbReturn;
                        }
                        changes.AddRange(command.Changes.Select(c => mapper.Map<GamePropertyChangeItemDto>(c)));
                    }
                }
                order.BinaryArray = JsonSerializer.SerializeToUtf8Bytes(changes);  //设置变化数据
            }
            else
                _Logger.LogDebug("交易物品产出为空。");

            order.Confirm2 = true;
            order.State = 1;
            order.CompletionDateTime = OwHelper.WorldNow;
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
                Amount = -order.Amount,
                CompletionDateTime = OwHelper.WorldNow,
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

        /// <summary>
        /// 问卷调查成功结束的回调。
        /// </summary>
        /// <param name="model">application/x-www-form-urlencoded 格式传递参数时，手字母小写。</param>
        /// <param name="mailManager"></param>
        /// <param name="blueprintManager"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<T78SurveiedReturnDto> Surveied([FromForm] T78SurveiedParamsDto model, [FromServices] GameMailManager mailManager, [FromServices] GameBlueprintManager blueprintManager)
        {
            _Logger.LogInformation("收到T78问卷调查回调，参数 = {model}", JsonSerializer.Serialize(model));
            T78SurveiedReturnDto result = new T78SurveiedReturnDto { };

            var dic = model.GetDictionary();
            var sign = _T78Manager.GetSignature(dic);
            if (sign != model.Sign) //若签名不正确
            {
                result.Result = 1;
                result.DebugMessage = $"签名不正确。";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            if (!Guid.TryParse(model.TId, out var tid))
            {
                result.Result = 1;
                result.DebugMessage = $"model.TId 是无效的TId。";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            using (var dwKey = _GameAccountStore.GetOrLoadUser(model.UserId, model.UserId, out var user))
            {
                //using var dwKey1 = _GameAccountStore.GetOrLoadUser("string306", "string", out user);
                if (dwKey.IsEmpty) goto lbErr;
                var gc = user.CurrentChar;
                //购买商品的输出项
                var tt = _ShoppingManager.GetShoppingTemplateByTId(tid);
                if (tt is null) goto lbErr; //若找不到模板
                var now = OwHelper.WorldNow;
                if (!_ShoppingManager.IsMatch(gc, tt, now, out _)) goto lbErr;    //若不能购买

                var allEntity = _EntityManager.GetAllEntity(gc)?.ToArray();
                if (allEntity is null) goto lbErr;  //若无法获取
                                                    //提前缓存产出项
                                                    //消耗项
                var periodIndex = _SearcherManager.GetPeriodIndex(tt.ShoppingItem.Ins, gc, out _); //提前获取自周期数

                if (tt.ShoppingItem.Ins.Count > 0)  //若需要消耗资源
                    if (!blueprintManager.Deplete(allEntity, tt.ShoppingItem.Ins)) goto lbErr;

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
                //发邮件
                using (var dwKeyAdmin = _GameAccountStore.GetOrLoadUser(ProjectContent.AdminLoginName, ProjectContent.AdminPwd, out var adminUser))
                {
                    if (dwKeyAdmin.IsEmpty) goto lbErr;
                    var command = new SendMailCommand
                    {
                        GameChar = adminUser.CurrentChar,
                        Mail = new SendMailItem
                        {
                            Body = "None",
                            Subject = "None",
                        },
                    };
                    command.ToIds.Add(gc.Id);   //加入收件人
                    command.Mail.Attachment.AddRange(items);    //加入附件
                    _SyncCommandManager.Handle(command);
                    if (command.HasError)
                    {
                        result.Result = 1;
                        result.FillErrorFrom(command);
                        _Logger.LogWarning(result.DebugMessage);
                        return result;
                    }
                }
                var historyItem = _ShoppingManager.CreateHistoryItem(gc);
                historyItem.TId = tid;
                historyItem.Count = 1;
                historyItem.WorldDateTime = now;
                historyItem.PeriodIndex = periodIndex;
                _ShoppingManager.AddHistoryItem(historyItem, gc);
                _Logger.LogInformation("收到T78问卷调查回调，正常发送奖励。CharId={gcId}", gc.Id);
                return result;
            }
        lbErr:
            result.Result = 1;
            result.FillErrorFromWorld();
            _Logger.LogWarning(result.DebugMessage);
            return result;
        }
    }

}
