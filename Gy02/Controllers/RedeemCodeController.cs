using GY02;
using GY02.Publisher;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gy02.Controllers
{
    /// <summary>
    /// 兑换码功能控制器。
    /// </summary>
    public class RedeemCodeController : GameControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public RedeemCodeController()
        {

        }

        /// <summary>
        /// 生成兑换码。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GenerateRedeemCodeReturnDto> GenerateRedeemCode(GenerateRedeemCodeParamsDto model)
        {
            var result = new GenerateRedeemCodeReturnDto();

            return result;
        }
    }

    /// <summary>
    /// 生成兑换码功能参数封装类。
    /// </summary>
    public class GenerateRedeemCodeParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 生成兑换码功能返回值封装类。
    /// </summary>
    public class GenerateRedeemCodeReturnDto : ReturnDtoBase
    {
    }
}
