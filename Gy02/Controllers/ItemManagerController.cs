using AutoMapper;
using Gy02.Publisher;
using Gy02Bll.Commands;
using Gy02Bll.Managers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query;
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

        readonly IServiceProvider _ServiceProvider;

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

#if DEBUG

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<bool> Test()
        {
            var store = _ServiceProvider.GetRequiredService<GameAccountStore>();
            store.LoadOrGetUser("string56", "string", out var gu);
            var token = gu.Token;
            var model = new AddItemsParamsDto { Token = token };
            model.TIds.Add(Guid.Parse("c531cd34-fed9-4859-81f1-501c25c0926d"));
            model.Counts.Add(2);
            AddItems(model, store, _ServiceProvider.GetRequiredService<SyncCommandManager>(), _ServiceProvider.GetRequiredService<TemplateManager>(),
                _ServiceProvider.GetRequiredService<IMapper>());
            return true;
        }
#endif

        /// <summary>
        /// 增加物品，调试用接口。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="store"></param>
        /// <param name="commandManager"></param>
        /// <param name="templateManager"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<AddItemsReturnDto> AddItems(AddItemsParamsDto model, [FromServices] GameAccountStore store, [FromServices] SyncCommandManager commandManager,
            [FromServices] TemplateManager templateManager, [FromServices] IMapper mapper)
        {
            using var dw = store.GetCharFromToken(model.Token, out var gc);
            var result = new AddItemsReturnDto { };
            if (dw.IsEmpty)
            {
                result.FillErrorFromWorld();
                return result;
            }
            var list = new List<GameEntity>();
            for (int i = 0; i < model.TIds.Count; i++)
            {
                var item = model.TIds[i];
                var tmp = CreateThing(item, model.Counts[i], templateManager, commandManager);
                if (tmp is null) //若出错
                {
                    result.FillErrorFromWorld();
                    return result;
                }
                var coll = tmp.Select(c => templateManager.GetEntityBase(c, out _)).OfType<GameEntity>();
                if (coll is null || coll.Count() != tmp.Count)
                    continue;
                list.AddRange(coll);
            }
            var command = new MoveEntitiesCommand { Items = list, Container = null, GameChar = gc };
            commandManager.Handle(command);
            mapper.Map<AddItemsReturnDto>(command);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tid"></param>
        /// <param name="count"></param>
        /// <param name="templateManager"></param>
        /// <param name="commandManager"></param>
        /// <returns></returns>
        public static List<VirtualThing>? CreateThing(Guid tid, decimal count, TemplateManager templateManager, SyncCommandManager commandManager)
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
                    commandManager.Handle(command);
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

        /// <summary>
        /// 升级装备/物品。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LvUpReturnDto> LvUp(LvUpParamsDto model)
        {
            var mapper = _ServiceProvider.GetRequiredService<IMapper>();
            var result = new LvUpReturnDto { };
            var store = _ServiceProvider.GetRequiredService<GameAccountStore>();
            using var dw = store.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                result.FillErrorFromWorld();
                return result;
            }
            var command = mapper.Map<LvUpCommand>(model);
            command.GameChar = gc;
            var chm = _ServiceProvider.GetRequiredService<SyncCommandManager>();
            chm.Handle(command);
            mapper.Map(command, result);
            return result;
        }
    }

}
