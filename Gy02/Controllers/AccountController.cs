using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Manager;

namespace Gy02.Controllers
{
    /// <summary>
    /// 账号管理控制器。
    /// </summary>
    public class AccountController : GameControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        public GameCharManager CharManager { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="charManager"></param>
        public AccountController(GameCharManager charManager)
        {
            CharManager = charManager;
        }

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
