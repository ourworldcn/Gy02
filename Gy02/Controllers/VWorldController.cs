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
        /// 获取模板数据。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        [HttpPost]
        [ResponseCache(Duration = 120)]
        public ActionResult<GetTemplates2ReturnDto> GetTemplates2(GetTemplates2ParamsDto model, [FromServices] TemplateManager manager)
        {
            var result = new GetTemplates2ReturnDto();
            if (model.Uid != "gy001" || model.Pwd != "210115")
            {
                result.ErrorCode = ErrorCodes.Unauthorized;
                result.DebugMessage = "用户名或密码错误。";
                result.HasError = true;
            }
            else
                result.Templates = manager.Id2FullView.Values.ToArray();
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
