using AutoMapper;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Entity;
using OW.SyncCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GY02.Controllers
{
    /// <summary>
    /// 战斗控制器。
    /// </summary>
    public class CombatController : GameControllerBase
    {
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
        public CombatController(IServiceProvider service, GameAccountStoreManager gameAccountStore, GameCombatManager gameCombatManager, IMapper mapper, SyncCommandManager syncCommandManager) : base(service)
        {
            _GameAccountStore = gameAccountStore;
            _GameCombatManager = gameCombatManager;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
        }
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释

        GameAccountStoreManager _GameAccountStore;
        GameCombatManager _GameCombatManager;
        SyncCommandManager _SyncCommandManager;
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
        /// <returns></returns>
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

            if (model.ForceRefresh || !gc.TowerInfo.NormalId.HasValue) //若需要刷新
            {

            }

            //var command = new GetDurationCommand { GameChar = gc, };
            //_Mapper.Map(model, command);
            //_SyncCommandManager.Handle(command);
            //_Mapper.Map(command, result);
            _Mapper.Map(gc.TowerInfo, result.TowerInfo);
            return result;
        }
    }
}
