using Gy02.Publisher;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Entity;

namespace Gy02.Controllers
{
    /// <summary>
    /// 蓝图相关操作的控制器。
    /// </summary>
    public class BlueprintController : GameControllerBase
    {
        /// <summary>
        /// 使用指定蓝图。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ApplyBlueprintReturnDto> ApplyBlueprint(ApplyBlueprintParamsDto model)
        {
            var result = new ApplyBlueprintReturnDto { };
            return result;
        }
    }

}
