using GY02;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using OW.SyncCommand;
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
        public T0314Controller(ILogger<T0314Controller> logger, T0314Manager t0314Manager, GameAccountStoreManager gameAccountStore, IDbContextFactory<GY02UserContext> dbContextFactory)
        {
            _Logger = logger;
            _T0314Manager = t0314Manager;
            _GameAccountStore = gameAccountStore;
            _DbContextFactory = dbContextFactory;
            //捷游/东南亚服务器
        }

        ILogger<T0314Controller> _Logger;
        T0314Manager _T0314Manager;
        GameAccountStoreManager _GameAccountStore;
        IDbContextFactory<GY02UserContext> _DbContextFactory;

        /// <summary>
        /// 
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

            string str = "";
            _Logger.LogInformation("收到支付确认，参数:{str}", string.Join('&', model.GetDic().Select(c => c.Key + '=' + c.Value)));
            var ary = str.Split('&');
            if (ary.Length <= 0)
            {
                _Logger.LogWarning("没有内容");
                return "没有内容";
            }
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
            return "SUCCESS";
        }

        /// <summary>
        /// 
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
            using var db = _DbContextFactory.CreateDbContext();
            var order = new GameShoppingOrder { };

            //var command = new GetShoppingItemsCommand { GameChar = gc, };

            return result;
        }
    }

    /// <summary>
    /// 确认订单功能的参数封装类。
    /// </summary>
    public class EnsureT0314OrderParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 游戏下单时传递的游戏订单号。
        /// </summary>
        public string OrderNo { get; set; } = null!;

        /// <summary>
        /// 购买的商品模板Id。
        /// </summary>
        public Guid GoodsTId { get; set; }
    }

    /// <summary>
    /// 确认订单功能的返回值封装类。
    /// </summary>
    public class EnsureT0314OrderReturnDto : ReturnDtoBase
    {
    }
}
