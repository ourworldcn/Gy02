using AutoMapper;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.AspNetCore.Mvc;
using OW.SyncCommand;

namespace GY02.Controllers
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
        /// <param name="blueprintManager"></param>
        public BlueprintController(BlueprintManager blueprintManager, SyncCommandManager syncCommandManager, IMapper mapper)
        {
            _BlueprintManager = blueprintManager;
            _SyncCommandManager = syncCommandManager;
            _Mapper = mapper;
        }

        private BlueprintManager _BlueprintManager;
        SyncCommandManager _SyncCommandManager;
        IMapper _Mapper;

        /// <summary>
        /// 使用指定蓝图。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ApplyBlueprintReturnDto> ApplyBlueprint(ApplyBlueprintParamsDto model)
        {
            var result = new ApplyBlueprintReturnDto { };
            var command = new CompositeCommand { };

            _SyncCommandManager.Handle(command);

            _Mapper.Map(command, result);
            return result;
        }
    }

}
