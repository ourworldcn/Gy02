using GY02;
using GY02.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public T0314Controller(ILogger<T0314Controller> logger, T0314Manager t0314Manager)
        {
            _Logger = logger;
            _T0314Manager = t0314Manager;
            //捷游/东南亚服务器
        }

        ILogger<T0314Controller> _Logger;
        T0314Manager _T0314Manager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<string> Payed([FromForm] string str)
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
            _Logger.LogInformation("收到支付确认，参数{str}", str);
            var ary = str.Split('&');
            if (ary.Length <= 0)
            {
                _Logger.LogWarning("没有内容");
                return "没有内容";
            }
            var dic = new Dictionary<string, string>();
            foreach (var item in ary)
            {
                var kv = item.Split('=');
                if (kv.Length != 2)
                {
                    _Logger.LogWarning("格式错误");
                    return "格式错误";
                }
                dic[kv[0]] = kv[1];
            }
            var sign = _T0314Manager.GetSign(dic);
            var signStr = dic["sign"];
            if (string.Compare(Convert.ToHexString(sign), signStr, StringComparison.OrdinalIgnoreCase) != 0)
            {
                _Logger.LogWarning("签名错误");
                return "签名错误";
            }
            return "SUCCESS";   //FAILED
        }
    }
}
