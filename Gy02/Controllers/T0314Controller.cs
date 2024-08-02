using AutoMapper;
using GY02;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Writers;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
using OW.SyncCommand;
using System;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Gy02.Controllers
{
    /// <summary>
    /// T0314合作伙伴相关控制器。
    /// </summary>
    public class T0314Controller : GameControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public T0314Controller(ILogger<T0314Controller> logger, T0314Manager t0314Manager, GameAccountStoreManager gameAccountStore,
            IDbContextFactory<GY02UserContext> dbContextFactory, SyncCommandManager syncCommandManager, IMapper mapper, GameEntityManager entityManager,
            SpecialManager specialManager, GameTemplateManager templateManager, IHostEnvironment environment)
        {
            _Logger = logger;
            _T0314Manager = t0314Manager;
            _GameAccountStore = gameAccountStore;
            _DbContextFactory = dbContextFactory;
            _SyncCommandManager = syncCommandManager;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _SpecialManager = specialManager;
            _TemplateManager = templateManager;
            _Environment = environment;
            //捷游/东南亚服务器
        }

        ILogger<T0314Controller> _Logger;
        T0314Manager _T0314Manager;
        GameAccountStoreManager _GameAccountStore;
        IDbContextFactory<GY02UserContext> _DbContextFactory;
        SyncCommandManager _SyncCommandManager;
        IMapper _Mapper;
        GameEntityManager _EntityManager;
        SpecialManager _SpecialManager;
        GameTemplateManager _TemplateManager;
        IHostEnvironment _Environment;

        /// <summary>
        /// 安卓支付回调地址。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<string> Payed([FromForm] T0314PayReturnStringDto model)
        {
            /*
             * a)假设本地计算的签名与POST中传递的签名一致，则通过
             * b)若签名不一致，则返回FAILED，中断处理。
             * CP应判断是否已发送道具。
             * CP其他判断逻辑。
             * 处理完成后。
             * a)希望SDK继续通知则返回任何非SUCCESS的字符。
             * b)处理完毕，订单结束则返回SUCCESS，SDK不会再通知。
             * https://abb.shfoga.com:20443/api/T0314/Payed
             */

            //string str = "";
            _Logger.LogInformation("收到支付确认，参数:{str}", string.Join('&', model.GetDic().Select(c => c.Key + '=' + c.Value)));
            //var ary = str.Split('&');
            //if (ary.Length <= 0)
            //{
            //    _Logger.LogWarning("没有内容");
            //    return "没有内容";
            //}
            Dictionary<string, string> dic;
            var keys = Request.Form.Select(c => c.Key).ToHashSet();
            try
            {
                dic = model.GetDic();
                var removes = dic.Keys.Except(keys).ToArray();
                removes.ForEach(c => dic.Remove(c));
            }
            catch (Exception)
            {
                _Logger.LogWarning("格式错误");
                return "格式错误";
            }
            var sign = _T0314Manager.GetSignForAndroid(dic);
            var localSign = Convert.ToHexString(sign);
            _Logger.LogInformation("计算获得签名为: {str}", localSign);
            var signStr = dic["sign"];
            if (string.Compare(Convert.ToHexString(sign), signStr, StringComparison.OrdinalIgnoreCase) != 0)
            {
                _Logger.LogWarning("签名错误");
                return "FAILED";
            }
            if (!Guid.TryParse(model.CpOrderNo, out var orderId))
            {
                _Logger.LogWarning("非法的开发者订单Id:{CpOrderNo}", model.CpOrderNo);
                return "非法的开发者订单Id";
            }
            //验证用户
            using var db = _DbContextFactory.CreateDbContext();
            var guThing = db.VirtualThings.FirstOrDefault(c => c.ExtraGuid == ProjectContent.UserTId && c.ExtraString == model.Uid);
            if (guThing is null)
            {
                _Logger.LogWarning("找不到指定用户{uid}", model.Uid);
                return "找不到指定用户";
            }
            var gcThing = guThing.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.CharTId);
            if (gcThing is null)
            {
                _Logger.LogWarning("用户数据损坏{uid}", model.Uid);
                return "用户数据损坏";
            }
            var order = db.ShoppingOrder.FirstOrDefault(c => c.Id == orderId);
            if (order is null)  //若新建
            {
                order = new GameShoppingOrder
                {
                    Id = orderId,
                    CustomerId = gcThing.IdString,  //角色Id
                    Currency = model.PayCurrency,
                    State = 0,
                };
                if (!OwConvert.TryToDecimal(model.PayAmount, out var amount))
                {
                    _Logger.LogWarning("支付金额非法{amount}", model.PayAmount);
                    return "找不到指定用户";
                }
                order.Amount = amount;
                db.ShoppingOrder.Add(order);
            }
            else if (order.State != 0)  //若订单已经完成
            {
                _Logger.LogWarning("订单已经完成不可重复通知{id}", model.CpOrderNo);
                return "订单已经完成不可重复通知";
            }
            order.Confirm2 = true;
            if (order.Confirm1) order.State = 1;
            //容错
            order.Currency ??= model.PayCurrency;
            if (order.Amount == 0 && decimal.TryParse(model.PayAmount, out var amount1)) order.Amount = amount1;
            db.SaveChanges();
            _Logger.LogDebug("订单已经成功入库,Id={id}", model.CpOrderNo);
            return "SUCCESS";
        }

        /// <summary>
        /// Ios支付回调地址。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<string> PayedForIos([FromForm] T0314PayReturnStringDto model)
        {
            /*
             * a)假设本地计算的签名与POST中传递的签名一致，则通过
             * b)若签名不一致，则返回FAILED，中断处理。
             * CP应判断是否已发送道具。
             * CP其他判断逻辑。
             * 处理完成后。
             * a)希望SDK继续通知则返回任何非SUCCESS的字符。
             * b)处理完毕，订单结束则返回SUCCESS，SDK不会再通知。
             * https://abb.shfoga.com:20443/api/T0314/PayedForIos
             * 测试的回调地址 https://47.237.87.21:20443/api/T0314/PayedForIos
             */

            //string str = "";
            _Logger.LogInformation("收到支付确认，参数:{str}", string.Join('&', model.GetDic().Select(c => c.Key + '=' + c.Value)));
            //var ary = str.Split('&');
            //if (ary.Length <= 0)
            //{
            //    _Logger.LogWarning("没有内容");
            //    return "没有内容";
            //}
            Dictionary<string, string> dic;
            var keys = Request.Form.Select(c => c.Key).ToHashSet();
            try
            {
                dic = model.GetDic();
                var removes = dic.Keys.Except(keys).ToArray();
                removes.ForEach(c => dic.Remove(c));
            }
            catch (Exception)
            {
                _Logger.LogWarning("格式错误");
                return "格式错误";
            }
            var sign = _T0314Manager.GetSignForIos(dic);
            var localSign = Convert.ToHexString(sign);
            _Logger.LogInformation("计算获得签名为: {str}", localSign);
            var signStr = dic["sign"];
            if (string.Compare(Convert.ToHexString(sign), signStr, StringComparison.OrdinalIgnoreCase) != 0)
            {
                _Logger.LogWarning("签名错误");
                return "FAILED";
            }
            if (!Guid.TryParse(model.CpOrderNo, out var orderId))
            {
                _Logger.LogWarning("非法的开发者订单Id:{CpOrderNo}", model.CpOrderNo);
                return "非法的开发者订单Id";
            }
            //验证用户
            using var db = _DbContextFactory.CreateDbContext();
            var guThing = db.VirtualThings.FirstOrDefault(c => c.ExtraGuid == ProjectContent.UserTId && c.ExtraString == model.Uid);
            if (guThing is null)
            {
                _Logger.LogWarning("找不到指定用户{uid}", model.Uid);
                return "找不到指定用户";
            }
            var gcThing = guThing.Children.FirstOrDefault(c => c.ExtraGuid == ProjectContent.CharTId);
            if (gcThing is null)
            {
                _Logger.LogWarning("用户数据损坏{uid}", model.Uid);
                return "用户数据损坏";
            }
            var order = db.ShoppingOrder.FirstOrDefault(c => c.Id == orderId);
            if (order is null)  //若新建
            {
                order = new GameShoppingOrder
                {
                    Id = orderId,
                    CustomerId = gcThing.IdString,  //角色Id
                    Currency = model.PayCurrency,
                    State = 0,
                };
                if (!OwConvert.TryToDecimal(model.PayAmount, out var amount))
                {
                    _Logger.LogWarning("支付金额非法{amount}", model.PayAmount);
                    return "找不到指定用户";
                }
                order.Amount = amount;
                db.ShoppingOrder.Add(order);
            }
            else if (order.State != 0)  //若订单已经完成
            {
                _Logger.LogWarning("订单已经完成不可重复通知{id}", model.CpOrderNo);
                return "订单已经完成不可重复通知";
            }
            order.Confirm2 = true;

            db.SaveChanges();
            _Logger.LogDebug("订单已经成功入库,Id={id}", model.CpOrderNo);
            var jo = order.GetJsonObject<T0314JObject>();
            if (_Environment.EnvironmentName != "jieyou" || jo.IsClientCreate)
                _T0314Manager.Reg(order.Id);
            return "SUCCESS";
        }

        /// <summary>
        /// 客户端调用的订单确认接口。可以在调用sdk服务器支付完成后调用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<EnsureT0314OrderReturnDto> EnsureT0314Order(EnsureT0314OrderParamsDto model)
        {
            var result = new EnsureT0314OrderReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            if (!Guid.TryParse(model.OrderNo, out var orderId))
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.HasError = true;
                result.DebugMessage = $"订单号格式非法——{model.OrderNo}";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
            var now = OwHelper.WorldNow;
            using var db = _DbContextFactory.CreateDbContext();
            GameShoppingOrder? order;
            for (order = db.ShoppingOrder.Find(orderId); order is null; order = db.ShoppingOrder.Find(orderId))
            {
                Thread.Sleep(1000);
                if (OwHelper.WorldNow - now > TimeSpan.FromSeconds(7))
                {
                    result.ErrorCode = ErrorCodes.WAIT_TIMEOUT;
                    result.HasError = true;
                    result.DebugMessage = $"订单确定时间超时，请重试——{model.OrderNo}";
                    _Logger.LogWarning(result.DebugMessage);
                    return result;
                }
            }
            if (order.State != 0)
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.HasError = true;
                result.DebugMessage = $"订单已经完成不可重复通知";
                _Logger.LogWarning(result.DebugMessage);
                return result;
            }
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
                GameChar = gc,
                Count = 1,
                ShoppingItemTId = model.GoodsTId,
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

            order.Confirm1 = true;
            //order.Confirm2 = true;
            if (order.Confirm1 && order.Confirm2)
                order.State = 1;
            else
                order.State = 2;
            order.CompletionDateTime = OwHelper.WorldNow;
            try
            {
                db.SaveChanges();
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

        /// <summary>
        /// 客户端创建并确认订单功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<CreateT0314OrderReturnDto> CreateT0314Order(CreateT0314OrderParamsDto model)
        {
            var result = new CreateT0314OrderReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            using var db = _DbContextFactory.CreateDbContext();

            GameShoppingOrder order = new GameShoppingOrder()
            {
                Confirm1 = true,
                CustomerId = gc.GetThing().IdString,
            };
            #region 初始化数据对象信息
            var jo = order.GetJsonObject<T0314JObject>();
            jo.TId = model.ShoppingItemTId;
            jo.IsClientCreate = true;
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
            db.Add(order);
            db.SaveChanges();
            result.OrderNo = order.Id.ToString();

            return result;
        }

        /// <summary>
        /// 获取指定订单。
        /// </summary>
        /// <returns></returns>
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetT0314OrderReturnDto> GetT0314Order([FromQuery] GetT0314OrderParamsDto model)
        {
            var result = new GetT0314OrderReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            if (!Guid.TryParse(model.OrderNo, out var orderId))
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = $"订单号格式错误，{nameof(model.OrderNo)}={model.OrderNo}";
                result.HasError = true;
                return result;
            }
            using var db = _DbContextFactory.CreateDbContext();
            var order = db.ShoppingOrder.Find(orderId);
            if (order is null)
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = $"无此订单，{nameof(model.OrderNo)}={model.OrderNo}";
                result.HasError = true;
                return result;
            }
            if (Guid.TryParse(order.CustomerId, out var gcId))
                if (gcId != gc.Id)    //若不是自己的订单
                {
                    result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    result.DebugMessage = $"只能查询自己的订单。";
                    result.HasError = true;
                    return result;
                }
            var jo = order.GetJsonObject<T0314JObject>();
            if (jo.NotifyDateTime is null)  //若未记录客户端查询时间
            {
                jo.NotifyDateTime = OwHelper.WorldNow;
            }
            var command = new ShoppingBuyCommand();
            if (jo.SendInMail is null)   //若尚未发送商品
            {
                if (order.State == 1)  //若可以发送奖品
                {
                    #region 内部购买
                    var tt = _TemplateManager.GetFullViewFromId(jo.TId);
                    if (tt is null)
                    {
                        result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                        result.DebugMessage = $"无此商品，商品模板Id={jo.TId}";
                        result.HasError = true;
                        return result;
                    }

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
                    command = new ShoppingBuyCommand
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

                    jo.SendInMail = false;
                }
            }
            db.SaveChanges();
            result.Order = _Mapper.Map<GameShoppingOrderDto>(order);
            _Mapper.Map(jo.EntitySummaries, result.EntitySummaryDtos);
            _Mapper.Map(command.Changes, result.Order.Changes);
            return result;
        }

        #region TapTap相关
        /*
         * https://m60bysk56u.feishu.cn/docx/ZWPkd11xso08ZSxYyDTcr10dnoc?from=from_copylink
         * https://developer.taptap.io/docs/zh-Hans/sdk/
         * https://developer.taptap.io/docs/zh-Hans/tap-download/
        */

        /// <summary>
        /// Taptap支付回调。
        /// </summary>
        /// <remarks>参考 https://developer.taptap.io/docs/zh-Hans/sdk/TapPayments/develop/server/
        /// POST[application/json; charset=utf-8]
        /// 同样的通知可能会多次发送，商户系统必须正确处理重复通知。
        /// 推荐做法是：当商户系统收到通知时，首先进行签名验证，然后检查对应业务数据的状态，如未处理，则进行处理；如已处理，则直接返回成功。
        /// 在处理业务数据时建议采用数据锁进行并发控制，以避免可能出现的数据异常。</remarks>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<T0314TapTapPayedReturnDto> T0314TapTapPayed(T0314TapTapPayedParamsDto model)
        {
            var result = new T0314TapTapPayedReturnDto();

            result.Code = "SUCCESS";
            return result;
        }

        /// <summary>
        /// 创建 T0314TapTap 订单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">无效令牌。</response> 
        [HttpPost]
        public ActionResult<CreateT0314TapTapOrderReturnDto> CreateT0314TapTapOrder(CreateT0314TapTapOrderParamsDto model)
        {
            var result = new CreateT0314TapTapOrderReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            using var db = _DbContextFactory.CreateDbContext();

            GameShoppingOrder order = new GameShoppingOrder()
            {
                Confirm1 = true,
                CustomerId = gc.GetThing().IdString,
            };

            #region 初始化数据对象信息
            var jo = order.GetJsonObject<T0314JObject>();
            jo.TId = model.ShoppingItemTId;
            jo.IsClientCreate = true;
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

            db.Add(order);
            db.SaveChanges();
            result.OrderId = order.Id;

            return result;
        }

        /// <summary>
        /// 查询TapTap订单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">无效令牌。</response> 
        [HttpGet]
        public ActionResult<GetT0314TapTapOrderReturnDto> GetT0314TapTapOrder([FromQuery] GetT0314TapTapOrderParamsDto model)
        {
            var result = new GetT0314TapTapOrderReturnDto();
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            var now = OwHelper.WorldNow;
            var endDt = now + TimeSpan.FromSeconds(6);
            using var db = _DbContextFactory.CreateDbContext();
            GameShoppingOrder? order = null;
            for (var tmp = now; tmp <= endDt; tmp = OwHelper.WorldNow)
            {
                order = db.ShoppingOrder.Find(model.OrderId);
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

            }
            //_Mapper.Map(command.Changes, result.Order.Changes);
            return result;
        }
        #endregion TapTap相关

    }

}
