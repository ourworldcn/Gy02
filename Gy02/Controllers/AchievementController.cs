using AutoMapper;
using GY02;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.AspNetCore.Mvc;
using OW.SyncCommand;

namespace Gy02.Controllers
{
    /// <summary>
    /// 成就功能控制器。
    /// </summary>
    public class AchievementController : GameControllerBase
    {

        /// <summary>
        /// 构造函数。
        /// </summary>
        public AchievementController(GameAccountStoreManager accountStore, IMapper mapper, SyncCommandManager syncCommandManager)
        {
            _AccountStore = accountStore;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
        }

        private GameAccountStoreManager _AccountStore;

        IMapper _Mapper;

        SyncCommandManager _SyncCommandManager;

        /// <summary>
        /// 获取指定成就的状态。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetAchievementStateReturnDto> GetAchievementState(GetAchievementStateParamsDto model)
        {
            var result = new GetAchievementStateReturnDto { };
            using var dw = _AccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new GetAchievementStateCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }

        /// <summary>
        /// 获取成就奖励功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetAchievementRewardsReturnDto> GetAchievementRewards(GetAchievementRewardsParamsDto model)
        {
            var result = new GetAchievementRewardsReturnDto { };
            using var dw = _AccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new GetAchievementRewardsCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }
    }

}
