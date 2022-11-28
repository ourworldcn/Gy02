using Gy02Bll.Templates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gy02.Controllers
{
    /// <summary>
    /// 虚拟世界公用Api控制器。
    /// </summary>
    public class VWorldController : GameControllerBase
    {
        /// <summary>
        /// 获取所有模板。
        /// </summary>
        /// <returns><seealso cref="Gy02TemplateJO"/></returns>
        [HttpGet]
        [ResponseCache(Duration = 120)]
        public ActionResult<IEnumerable<Gy02TemplateJO>> GetTemplates()
        {
            return Array.Empty<Gy02TemplateJO>().ToList();
        }
    }
}
