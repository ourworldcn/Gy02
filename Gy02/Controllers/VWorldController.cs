using Gy02.Publisher;
using Gy02Bll.Templates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Managers;
using System.Text.Json;

namespace Gy02.Controllers
{
    /// <summary>
    /// 虚拟世界公用Api控制器。
    /// </summary>
    public class VWorldController : GameControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        public VWorldController()
        {
        }

        /// <summary>
        /// 获取所有模板。
        /// </summary>
        /// <param name="model">入参。</param>
        /// <param name="manager"></param>
        /// <returns><seealso cref="Gy02TemplateJO"/></returns>
        [HttpGet]
        [ResponseCache(Duration = 120)]
        public ActionResult<GetTemplatesReturnDto> GetTemplates(GetTemplatesParamsDto model, [FromServices] TemplateManager manager)
        {
            var result = new GetTemplatesReturnDto();
            if (model.Uid != "gy001" || model.Pwd != "210115")
            {
                result.ErrorCode = ErrorCodes.Unauthorized;
            }
            else
                result.Templates = manager.Id2Template.Values.Select(c => c.GetJsonObject<Gy02TemplateJO>()).ToArray();
            return result;
        }

        /// <summary>
        /// 重新启动服务。通常用于更新数据后重启。
        /// </summary>
        /// <param name="applicationLifetime"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<bool> StopService([FromServices] IHostApplicationLifetime applicationLifetime)
        {
            Global.Program.ReqireReboot = true;
            applicationLifetime.StopApplication();
            return true;
        }
    }
}
