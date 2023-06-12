using GY02.Publisher;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.Game.Managers;
using OW.GameDb;

namespace GY02.Controllers
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
        [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any)]
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
        /// 使用缓存获取配置。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        [HttpGet]
        [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new string[] { nameof(GetTemplates2ParamsDto.Uid), nameof(GetTemplates2ParamsDto.Pwd) })]
        public ActionResult<GetTemplates2ReturnDto> GetTemplates([FromQuery] GetTemplates2ParamsDto model, [FromServices] TemplateManager manager)
        {
            var result = new GetTemplates2ReturnDto();
            if (model.Uid != "gy001" || model.Pwd != "210115")
            {
                result.ErrorCode = ErrorCodes.Unauthorized;
                result.DebugMessage = "用户名或密码错误。";
                result.HasError = true;
                return Unauthorized(result);
            }
            else
                result.Templates = manager.Id2FullView.Values.ToArray();
            return result;
        }


        /// <summary>
        /// 关闭服务并写入所有缓存数据并重新启动服务器。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="applicationLifetime"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<StopServiceReturnDto> RebootService(StopServiceParamsDto model, [FromServices] IHostApplicationLifetime applicationLifetime)
        {
            var result = new StopServiceReturnDto();
            if (model.UId != "gy001" || model.Pwd != "210115")
            {
                result.ErrorCode = ErrorCodes.Unauthorized;
                result.DebugMessage = "用户名或密码错误。";
                result.HasError = true;
            }
            var dbLogging = HttpContext.RequestServices.GetService<GameSqlLoggingManager>();
            if (dbLogging is not null)
            {
                var actionRecord = new GameActionRecord
                {
                    ActionId = "Reboot",
                    JsonObjectString = $"{{\"UId\":\"{model.UId}\"}}",
                };
                dbLogging.AddLogging(actionRecord);
            }
            Global.Program.ReqireReboot = true;
            applicationLifetime.StopApplication();
            return result;
        }

        /// <summary>
        /// 获取服务器时间接口。
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<GetServerDateTimeUtcReturnDto> GetServerDateTimeUtc()
        {
            var result = new GetServerDateTimeUtcReturnDto { DateTimeUtc = DateTime.UtcNow };
            return result;
        }
    }

}
