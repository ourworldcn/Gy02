﻿using GY02;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        public T78Controller(PublisherT78Manager t78Manager)
        {
            _T78Manager = t78Manager;
        }

        PublisherT78Manager _T78Manager;

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
