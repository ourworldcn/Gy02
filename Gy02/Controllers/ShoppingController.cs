using Gy02Bll.Templates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gy02.Controllers
{
    /// <summary>
    /// 商城功能控制器。
    /// </summary>
    public class ShoppingController : GameControllerBase
    {
        /// <summary>
        /// 获取指定商品配置数据。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<List<GameShoppingItem>> GetItems()
        {
            return Ok();
        }
    }
}
