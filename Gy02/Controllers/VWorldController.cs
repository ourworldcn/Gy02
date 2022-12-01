using Gy02Bll.Templates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Managers;

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
        public ActionResult<IEnumerable<Gy02TemplateJO>> GetTemplates([FromServices]TemplateManager manager)
        {
            return manager.Id2Template.Values.Select(c=>c.GetJsonObject<Gy02TemplateJO>()).ToArray();
        }
    }
}
