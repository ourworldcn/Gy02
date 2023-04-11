using AutoMapper;
using Gy02.Publisher;
using Gy02Bll.Base;
using Gy02Bll.Commands;
using Gy02Bll.Managers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage.Internal;
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
        public ActionResult<bool> TestAddItems()
        {
            var store = _ServiceProvider.GetRequiredService<GameAccountStore>();
            store.LoadOrGetUser("gy20", "HtnXNCiJ", out var gu);
            var token = gu.Token;
            var model = new AddItemsParamsDto { Token = token };
            //加金币
            model.TIds.Add(ProjectContent.GoldTId);
            model.Counts.Add(200_000);
            //加图纸
            model.TIds.Add(Guid.Parse("16a8b068-918b-46ad-8ae6-d3797c7683ac"));
            model.Counts.Add(100);
            AddItems(model, store, _ServiceProvider.GetRequiredService<SyncCommandManager>(), _ServiceProvider.GetRequiredService<TemplateManager>(),
                _ServiceProvider.GetRequiredService<IMapper>());
            return true;
        }

        /// <summary>
        /// 测试升级接口。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<bool> TestLvUp()
        {
            var store = _ServiceProvider.GetRequiredService<GameAccountStore>();
            store.LoadOrGetUser("gy20", "HtnXNCiJ", out var gu);
            var token = gu.Token;
            var item = gu.CurrentChar.ZhuangBeiBag.Children.First(c => c.TemplateId == Guid.Parse("402a7b1c-bd32-4540-9efe-f9801ed6946b"));
            var sub = new LvUpParamsDto { Token = token, };
            sub.Ids.Add(item.Id);
            LvUp(sub);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<bool> TestComp()
        {
            var store = _ServiceProvider.GetRequiredService<GameAccountStore>();
            store.LoadOrGetUser("gy27", "W1QPLWSB", out var gu);
            var token = gu.Token;
            var gc = gu.CurrentChar;
            var model = new CompositeParamsDto { Token = token, BlueprintId = Guid.Parse("2fc38d58-86e5-4eff-ba68-ccf60f356f5a") };

            var items = gc.GetAllChildren().Where(c => c.ExtraGuid == Guid.Parse("110b74ff-db7e-4a28-9e28-12beaf18a9e1")).Take(3).ToArray(); //兰山羊
            model.MainId = items.First().Id;
            model.Ids.AddRange(items.Skip(1).Select(c => c.Id));

            Composite(model);
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
                var tmp = CreateVirtualThingHandler.CreateThing(item, model.Counts[i], templateManager, commandManager);
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
            mapper.Map(command, result);
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

        /// <summary>
        /// 物品降级接口。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LvDownReturnDto> LvDown(LvDownParamsDto model)
        {
            var mapper = _ServiceProvider.GetRequiredService<IMapper>();
            var tm = _ServiceProvider.GetRequiredService<TemplateManager>();
            var result = new LvDownReturnDto { };
            var store = _ServiceProvider.GetRequiredService<GameAccountStore>();
            using var dw = store.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                result.FillErrorFromWorld();
                return result;
            }
            var command = mapper.Map<LvDownCommand>(model);
            command.GameChar = gc;
            command.Entity = tm.GetEntityBase(gc.GetAllChildren().FirstOrDefault(c => c.Id == model.ItemId), out _) as GameEntity;
            var chm = _ServiceProvider.GetRequiredService<SyncCommandManager>();
            chm.Handle(command);
            mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 指定物品合成（升品阶）功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CompositeReturnDto> Composite(CompositeParamsDto model)
        {
            var mapper = _ServiceProvider.GetRequiredService<IMapper>();
            var result = new CompositeReturnDto { };
            var store = _ServiceProvider.GetRequiredService<GameAccountStore>();
            using var dw = store.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                result.FillErrorFromWorld();
                return result;
            }
            var command = mapper.Map<CompositeCommand>(model);
            command.GameChar = gc;
            var tm = _ServiceProvider.GetRequiredService<TemplateManager>();
            var entities = tm.GetEntityAndTemplateFullView<GameEntity>(command.GameChar, model.Ids);
            if (entities is null)
            {
                result.FillErrorFromWorld();
                return result;
            }
            command.Items.AddRange(entities);

            command.MainItem = tm.GetEntityBase(command.GameChar.GetAllChildren().FirstOrDefault(c => c.Id == model.MainId), out _) as GameEntity;
            var scm = _ServiceProvider.GetRequiredService<SyncCommandManager>();
            command.Blueprint = tm.Id2FullView[model.BlueprintId];

            scm.Handle(command);

            mapper.Map(command, result);
            return result;
        }

    }

}
