﻿using AutoMapper;
using GY02;
using GY02.Base;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.SyncCommand;
using System.Text;
using System.Text.Json;
using EnvironmentName = Microsoft.Extensions.Hosting.EnvironmentName;

namespace GY02.Controllers
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
        /// <param name="mapper"></param>
        /// <param name="gameEntityManager"></param>
        /// <param name="syncCommandManager"></param>
        /// <param name="gameAccountStore"></param>
        /// <param name="hostEnvironment"></param>
        public ItemManagerController(IServiceProvider serviceProvider, IMapper mapper, GameEntityManager gameEntityManager, SyncCommandManager syncCommandManager,
            GameAccountStoreManager gameAccountStore, IHostEnvironment hostEnvironment)
        {
            _ServiceProvider = serviceProvider;
            _Mapper = mapper;
            _EntityManager = gameEntityManager;
            _SyncCommandManager = syncCommandManager;
            _GameAccountStore = gameAccountStore;
            _HostEnvironment = hostEnvironment;
        }

        readonly IServiceProvider _ServiceProvider;
        IMapper _Mapper;
        GameEntityManager _EntityManager;
        SyncCommandManager _SyncCommandManager;
        GameAccountStoreManager _GameAccountStore;
        IHostEnvironment _HostEnvironment;

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
            if (command.ErrorCode == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
            var result = new MoveItemsReturnDto();
            mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 允许修改的物品的TId。
        /// </summary>
        static Guid[] tids = new Guid[] { ProjectContent.GuanggaoCurrenyTId };

        /// <summary>
        /// 增加广告币。以后此函数会过滤TId，仅允许增加特定的TId物品
        /// </summary>
        /// <returns></returns>
        /// <response code="401">令牌无效。</response>  
        [HttpPost]
        public ActionResult<AddItemForYourselfReturnDto> AddItemForYourself(AddItemForYourselfParamsDto model)
        {
            var result = new AddItemForYourselfReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            var errTid = tids.FirstOrDefault(c => !tids.Contains(c));
            if (errTid != Guid.Empty)
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = $"至少有一个物品不可以使用此接口更改，TId={errTid}";
                return result;
            }
            var changes = new List<GamePropertyChangeItem<object>> { };

            var entities = _Mapper.Map<List<GameEntitySummary>>(model.Entities);
            entities.ForEach(c => c.Id = null);
            //var entities = model.Entities.Select(c => _Mapper.Map<GameEntitySummary>(c));
            if (!_EntityManager.CreateAndMove(entities, gc, changes))
            {
                result.FillErrorFromWorld();
                return result;
            }
            _Mapper.Map(changes, result.Changes);
            return result;
        }

#if DEBUG

        /// <summary>
        /// 测试升级接口。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<bool> TestLvUp()
        {
            var store = _ServiceProvider.GetRequiredService<GameAccountStoreManager>();
            store.GetOrLoadUser("string404", "string", out var gu);
            var token = gu.Token;
            var item = gu.CurrentChar.ZhuangBeiBag.Children.First(c => c.TemplateId == Guid.Parse("402a7b1c-bd32-4540-9efe-f9801ed6946b"));
            var sub = new LvUpParamsDto { Token = token, };
            sub.Ids.Add(item.Id);
            LvUp(sub);
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
        public ActionResult<AddItemsReturnDto> AddItems(AddItemsParamsDto model, [FromServices] GameAccountStoreManager store, [FromServices] SyncCommandManager commandManager,
            [FromServices] GameTemplateManager templateManager, [FromServices] IMapper mapper)
        {
            var result = new AddItemsReturnDto { };
            if (_HostEnvironment.EnvironmentName != EnvironmentName.Development && _HostEnvironment.EnvironmentName != EnvironmentName.Production)
            {
                result.HasError = true;
                result.ErrorCode = ErrorCodes.ERROR_CALL_NOT_IMPLEMENTED;
                result.DebugMessage = "此环境下不能使用此功能。";
                return result;
            }
            using var dw = store.GetCharFromToken(model.Token, out var gc);

            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            var list = new List<GameEntity>();
            for (int i = 0; i < model.TIds.Count; i++)
            {
                var item = model.TIds[i];
                var tmp = _EntityManager.Create(new GameEntitySummary { TId = item, Count = model.Counts[i] });

                if (tmp is null) goto lbErr; //若出错
                tmp.ForEach(c =>    //强制增加数量
                {
                    c.Count = model.Counts[i];
                });
                if (tmp.Count() != tmp.Count) continue;
                list.AddRange(tmp);
            }
            List<GamePropertyChangeItem<object>> changes = new List<GamePropertyChangeItem<object>>();
            _EntityManager.Move(list, gc, changes);
            mapper.Map(changes, result.Changes);
            return result;
        lbErr:
            result.FillErrorFromWorld();
            return result;
        }

        #region 升降级相关

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
            var store = _ServiceProvider.GetRequiredService<GameAccountStoreManager>();
            using var dw = store.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
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
            var tm = _ServiceProvider.GetRequiredService<GameTemplateManager>();
            var result = new LvDownReturnDto { };
            var store = _ServiceProvider.GetRequiredService<GameAccountStoreManager>();
            using var dw = store.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            var command = mapper.Map<LvDownCommand>(model);
            command.GameChar = gc;
            //command.Entity = tm.GetEntityBase(gc.GetAllChildren().FirstOrDefault(c => c.Id == model.ItemId), out _) as GameEntity;
            var entity = _EntityManager.GetAllEntity(gc).FirstOrDefault(c => c.Id == model.ItemId);
            command.Entity = entity;
            var chm = _ServiceProvider.GetRequiredService<SyncCommandManager>();
            chm.Handle(command);
            mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 自动升级功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<AutoLvUpReturnDto> AutoLvUp(AutoLvUpParamsDto model)
        {
            var result = new AutoLvUpReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new AutoLvUpCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        #endregion 升降级相关

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
            var store = _ServiceProvider.GetRequiredService<GameAccountStoreManager>();
            using var dw = store.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            var command = mapper.Map<CompositeCommand>(model);
            command.GameChar = gc;
            command.RestoreLevel = true;
            var tm = _ServiceProvider.GetRequiredService<GameTemplateManager>();
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

        /// <summary>
        /// 自动合成紫色（不含）以下装备。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<AutoCompositeReturnDto> AutoComposite(AutoCompositeParamsDto model)
        {
            var result = new AutoCompositeReturnDto { };
            var store = _ServiceProvider.GetRequiredService<GameAccountStoreManager>();
            using var dw = store.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            var command = _Mapper.Map<AutoCompositeCommand>(model);
            command.GameChar = gc;
            var scm = _ServiceProvider.GetRequiredService<SyncCommandManager>();

            scm.Handle(command);

            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 分解（降品）装备。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<DecomposeReturnDto> Decompose(DecomposeParamsDto model)
        {
            var result = new DecomposeReturnDto { };
            var store = _ServiceProvider.GetRequiredService<GameAccountStoreManager>();
            using var dw = store.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            var command = new DecomposeCommand { GameChar = gc };

            //var item = gc.GetAllChildren().FirstOrDefault(c => c.Id == model.ItemId);
            //if (item is null)
            //{
            //    result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
            //    result.DebugMessage = $"找不到指定的装备，Id={model.ItemId}";
            //    return result;
            //}
            //var entity = _EntityManager.GetEntity(item);
            var entity = _EntityManager.GetAllEntity(gc).FirstOrDefault(c => c.Id == model.ItemId);
            if (entity is null)
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = $"找不到指定的装备，Id={model.ItemId}";
                return result;
            }
            command.Item = entity;

            _SyncCommandManager.Handle(command);

            _Mapper.Map(command, result);
            return result;

        }

        #region 孵化相关
#if DEBUG
        /// <summary>
        /// 测试孵化。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<FuhuaPreviewReturnDto> TestFuhuaPreview()
        {
            var src = new GameShoppingOrder { };
            var mapper = HttpContext.RequestServices.GetService<IMapper>()!;
            var dest = mapper.Map<GameShoppingOrderDto>(src);

            //var store = _ServiceProvider.GetRequiredService<GameAccountStoreManager>();
            //store.LoadOrGetUser("string40", "string", out var gu);
            //var token = gu.Token;
            //var gc = gu.CurrentChar;
            var model = new FuhuaPreviewParamsDto
            {
                //Token = token,
                ParentGenus = new List<string> { "zuoqi_wolf", "zuoqi_giraffe" },
            };

            return FuhuaPreview(model);

        }
#endif
        /// <summary>
        /// 孵化的预览功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<FuhuaPreviewReturnDto> FuhuaPreview(FuhuaPreviewParamsDto model)
        {
            var result = new FuhuaPreviewReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new FuhuaPreviewCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 孵化功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<FuhuaReturnDto> Fuhua(FuhuaParamsDto model)
        {
            var result = new FuhuaReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new FuhuaCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }
        #endregion 孵化相关

        /// <summary>
        /// 返回指定对象数据。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetEntitiesReturnDto> GetEntities(GetEntitiesParamsDto model)
        {
            var result = new GetEntitiesReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            var command = new GetEntitiesCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 修改指定实体的客户端用字典内容的功能。
        /// 获取字典可以使用GetEntities功能（返回实体中包含该字典）。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ModifyClientDictionaryReturnDto> ModifyClientDictionary(ModifyClientDictionaryParamsDto model)
        {
            var result = new ModifyClientDictionaryReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new ModifyClientDictionaryCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }
    }

}
