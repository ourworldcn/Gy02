using AutoMapper;
using Gy02.Publisher;
using Gy02Bll.Commands;
using Gy02Bll.Commands.Item;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Entity;
using OW.SyncCommand;

namespace Gy02.Controllers
{
    /// <summary>
    /// 蓝图相关操作的控制器。
    /// </summary>
    public class BlueprintController : GameControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public BlueprintController(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
        }

        IServiceProvider _ServiceProvider;

        /// <summary>
        /// 使用指定蓝图。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ApplyBlueprintReturnDto> ApplyBlueprint(ApplyBlueprintParamsDto model)
        {
            var result = new ApplyBlueprintReturnDto { };
            var command = new CompositeCommand {  };
            var commandManager = _ServiceProvider.GetRequiredService<SyncCommandManager>();
            commandManager.Handle(command);
            var mapper = _ServiceProvider.GetRequiredService<IMapper>();
            mapper.Map(command, result);
            return result;
        }
    }

}
