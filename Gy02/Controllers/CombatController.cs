using AutoMapper;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Entity;
using OW.Game.Managers;
using OW.SyncCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GY02.Controllers
{
    /// <summary>
    /// 战斗控制器。
    /// </summary>
    public class CombatController : GameControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="gameAccountStore"></param>
        /// <param name="gameCombatManager"></param>
        /// <param name="mapper"></param>
        /// <param name="syncCommandManager"></param>
        /// <param name="entityManager"></param>
        /// <param name="templateManager"></param>
        public CombatController(IServiceProvider service, GameAccountStoreManager gameAccountStore, GameCombatManager gameCombatManager, IMapper mapper,
            SyncCommandManager syncCommandManager, GameEntityManager entityManager, GameTemplateManager templateManager) : base(service)
        {
            _GameAccountStore = gameAccountStore;
            _GameCombatManager = gameCombatManager;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
            _EntityManager = entityManager;
            _TemplateManager = templateManager;
        }

        GameAccountStoreManager _GameAccountStore;
        GameCombatManager _GameCombatManager;
        SyncCommandManager _SyncCommandManager;
        GameEntityManager _EntityManager;
        GameTemplateManager _TemplateManager;
        IMapper _Mapper;
#if DEBUG
        /// <summary>
        /// 测试。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<bool> Test() { return true; }
#endif
        /// <summary>
        /// 开始战斗。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<StartCombatReturnDto> StartCombat(StartCombatParamsDto model)
        {
            var result = new StartCombatReturnDto { };

            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new StartCombatCommand { GameChar = gc };
            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);

            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 记录战斗中信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CombatMarkReturnDto> CombatMark(CombatMarkParamsDto model)
        {
            var result = new CombatMarkReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }


            var command = new CombatMarkCommand { GameChar = gc };
            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 结算战斗。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<EndCombatReturnDto> EndCombat(EndCombatParamsDto model)
        {
            var result = new EndCombatReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new EndCombatCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 获取区间。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetDurationReturnDto> GetDuration(GetDurationParamsDto model)
        {
            var result = new GetDurationReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new GetDurationCommand { GameChar = gc, };
            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;

        }

        /// <summary>
        /// 获取竞技场信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns>ErrorCode的错误意义如下：160=没有找到爬塔占位符（用此占位符来确定爬塔功能是否开启）。1219=无法购买刷新商品（可能是资源或次数不足。）</returns>
        [HttpPost]
        public ActionResult<GetTowerReturnDto> GetTower(GetTowerParamsDto model)
        {
            var result = new GetTowerReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }
            var ph = _EntityManager.GetEntity(_EntityManager.GetAllEntity(gc), new GameEntitySummary { TId = Guid.Parse("43ADC188-7B1D-4C73-983F-4E5583CBACCD") });    //爬塔占位符
            gc.TowerInfo ??= new TowerInfo();
            if (ph is null)
            {
                result.HasError = true;
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = $"没有找到爬塔占位符";
                return result;
            }
            var now = OwHelper.WorldNow.Date;
            if (!gc.TowerInfo.NormalId.HasValue || !gc.TowerInfo.RefreshDateTime.HasValue || gc.TowerInfo.RefreshDateTime.Value.Date != now)   //若需要免费刷新
            {
                var ids = _GameCombatManager.GetNewLevel(Guid.Empty);
                gc.TowerInfo.RefreshDateTime = now;
                gc.TowerInfo.EasyId = ids.Item1;
                gc.TowerInfo.IsEasyDone = null;
                gc.TowerInfo.NormalId = ids.Item2;
                gc.TowerInfo.IsNormalDone = null;
                gc.TowerInfo.HardId = ids.Item3;
                gc.TowerInfo.IsHardDone = null;
            }
            else if (model.ForceRefresh) //若需要强制刷新
            {
                var commandRefresh = new ShoppingBuyCommand
                {
                    GameChar = gc,
                    Count = 1,
                    ShoppingItemTId = Guid.Parse("E7A0E2A3-304E-4D19-84A4-14128650B152"),
                };
                _SyncCommandManager.Handle(commandRefresh);
                if (commandRefresh.HasError)
                {
                    result.FillErrorFrom(commandRefresh);
                    result.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                    return result;
                }
                _Mapper.Map(commandRefresh.Changes, result.Changes);
                var ids = _GameCombatManager.GetNewLevel(ph.Count == 0 ? Guid.Empty : _GameCombatManager.Towers[(int)ph.Count - 1].TemplateId);
                gc.TowerInfo.RefreshDateTime = now;
                gc.TowerInfo.EasyId = ids.Item1;
                gc.TowerInfo.IsEasyDone = null;
                gc.TowerInfo.NormalId = ids.Item2;
                gc.TowerInfo.IsNormalDone = null;
                gc.TowerInfo.HardId = ids.Item3;
                gc.TowerInfo.IsHardDone = null;
            }

            result.TowerInfo ??= new TowerInfoDto();
            _Mapper.Map(gc.TowerInfo, result.TowerInfo);
            result.TowerInfo.EasyTemplate = _TemplateManager.GetFullViewFromId(gc.TowerInfo.EasyId!.Value);
            result.TowerInfo.NormalTemplate = _TemplateManager.GetFullViewFromId(gc.TowerInfo.NormalId.Value);
            result.TowerInfo.HardTemplate = _TemplateManager.GetFullViewFromId(gc.TowerInfo.HardId!.Value);
            _GameAccountStore.Save(gc.GetUser().Key);
            return result;
        }
    }
}
