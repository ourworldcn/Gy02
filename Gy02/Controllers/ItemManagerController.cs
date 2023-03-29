using AutoMapper;
using Gy02.Publisher;
using Gy02Bll.Commands;
using Gy02Bll.Managers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.Store;
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

        /// <summary>
        /// 增加物品，调试用接口。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="store"></param>
        /// <param name="commandManager"></param>
        /// <param name="templateManager"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<AddItemsReturnDto> AddItems(AddItemsParamsDto model, [FromServices] GameAccountStore store, [FromServices] SyncCommandManager commandManager,
            [FromServices] TemplateManager templateManager)
        {
            using var dw = store.GetCharFromToken(model.Token, out var gc);
            var result = new AddItemsReturnDto { };
            if (dw.IsEmpty)
            {
                result.FillErrorFromWorld();
                return result;
            }
            List<(GameEntity, GameEntity)> list = new List<(GameEntity, GameEntity)> { };
            for (int i = 0; i < model.TIds.Count; i++)
            {
                var item = model.TIds[i];
                var tmp = CreateThing(item, model.Counts[i], templateManager, commandManager);
                if(tmp is null) //若出错
                {
                    result.FillErrorFromWorld();
                    return result;
                }
                //if (templateManager.GetEntityAndTemplate(sub.Result, out var entity, out var fullView))
                //{
                //    result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                //    return result;
                //}
                //list.Add((entity, model.Counts[i]));
            }
            if (list.Count > 0)
            {
                var sub = new MoveEntitiesCommand { };
                commandManager.Handle(sub);
                if (sub.HasError)
                {
                    result.FillErrorFrom(sub);
                    return result;
                }
            }
            return result;
        }

        List<VirtualThing>? CreateThing(Guid tid, decimal count, TemplateManager templateManager, SyncCommandManager commandManager)
        {
            var tt = templateManager.Id2FullView.GetValueOrDefault(tid);
            if (tt is null)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定Id的模板，TId={tid}");
                return null;
            }
            var result = new List<VirtualThing>();
            if (tt.Stk == 1)   //若不可堆叠
            {
                for (int i = 0; i < count; i++)
                {
                    var command = new CreateVirtualThingCommand { TemplateId = tid };
                    if (command.HasError)
                    {
                        OwHelper.SetLastError(command.ErrorCode);
                        OwHelper.SetLastErrorMessage(command.DebugMessage);
                        return null;
                    }
                    if (templateManager.GetEntityBase(command.Result, out _) is GameEntity ge)
                    {
                        ge.Count = 1;
                        result.Add(command.Result);
                    }
                    else
                    {
                        OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                        return null;
                    }
                    result.Add(command.Result);
                }
            }
            else //若可以堆叠
            {
                var command = new CreateVirtualThingCommand { TemplateId = tid };
                if (command.HasError)
                {
                    OwHelper.SetLastError(command.ErrorCode);
                    OwHelper.SetLastErrorMessage(command.DebugMessage);
                    return null;
                }
                if (templateManager.GetEntityBase(command.Result, out _) is GameEntity ge)
                {
                    ge.Count = count;
                    result.Add(command.Result);
                }
                else
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                    return null;
                }
            }
            return result;
        }
    }

}
