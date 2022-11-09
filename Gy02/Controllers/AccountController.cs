using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gy02.Controllers
{
    /// <summary>
    /// 账号管理控制器。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountController : GameControllerBase
    {
        /// <summary>
        /// 测试。
        /// </summary>
        /// <param name="str">测试参数。</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<bool> Test(string str)
        {
            return true;
        }
    }
}
