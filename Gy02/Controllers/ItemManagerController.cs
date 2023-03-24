using AutoMapper;
using Gy02.Publisher;
using Gy02Bll.Commands;
using Gy02Bll.Managers;
using Microsoft.AspNetCore.Mvc;
using OW.SyncCommand;

namespace Gy02.Controllers
{
    /// <summary>
    /// 物品管理控制器
    /// </summary>
    public class ItemManagerController : GameControllerBase
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public ItemManagerController(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
        }

        IServiceProvider _ServiceProvider;

        /// <summary>
        /// 移动物品接口。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="mapper"></param>
        /// <param name="commandMng"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost]
        public ActionResult<MoveItemsReturnDto> MoveItems(MoveItemsParamsDto model, [FromServices] IMapper mapper, [FromServices] SyncCommandManager commandMng)
        {
            var command = mapper.Map<MoveItemsCommand>(model);
            commandMng.Handle(command);
            var result = new MoveItemsReturnDto();
            mapper.Map(command, result);
            return result;
        }
    }

}
