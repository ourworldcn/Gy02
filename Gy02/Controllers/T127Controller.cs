using GY02;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public T127Controller(T127Manager t127Manager, IHttpClientFactory httpClientFactory)
        {
            _T127Manager = t127Manager;
            _HttpClientFactory = httpClientFactory;
        }

        T127Manager _T127Manager;
        IHttpClientFactory _HttpClientFactory;

        /// <summary>
        /// 通知服务器完成T127的订单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CompleteOrderReturnDto> CompleteOrder(CompleteOrderParamsDto model)
        {
            var result = new CompleteOrderReturnDto();
            var client = _HttpClientFactory.CreateClient();
            var r = _T127Manager.GetOrderState(model.ProductId, model.PurchaseToken);
            var state = JsonSerializer.Deserialize<T127OrderState>(r.Content.ReadAsStringAsync().Result);
            return result;
        }
    }

}
