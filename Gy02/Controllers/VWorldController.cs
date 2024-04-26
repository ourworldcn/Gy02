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
        public VWorldController(GameTemplateManager templateManager)
        {
            _TemplateManager = templateManager;
        }

        GameTemplateManager _TemplateManager;

        /// <summary>
        /// 获取配置数据。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<GetTemplates2ReturnDto> GetTemplates([FromQuery] GetTemplates2ParamsDto model)
        {
            var result = new GetTemplates2ReturnDto();
            if (model.Uid != "gy001" || model.Pwd != "210115")
            {
                result.ErrorCode = ErrorCodes.Unauthorized;
                result.DebugMessage = "用户名或密码错误。";
                result.HasError = true;
                return Unauthorized(result);
            }
            //取时间戳
            var modifyDateTime = Directory.GetLastWriteTimeUtc(Path.Combine(AppContext.BaseDirectory, "GameTemplates.json"));
            var timestamp = modifyDateTime.Ticks;
            if (!model.Timestamp.HasValue || model.Timestamp != timestamp)  //若需要返回数据
                result.Templates = _TemplateManager.Id2FullView.Values.ToArray();
            result.Timestamp = timestamp;
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
                var actionRecord = new ActionRecord
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
            var result = new GetServerDateTimeUtcReturnDto { DateTimeUtc = OwHelper.WorldNow };
            return result;
        }
    }

}
