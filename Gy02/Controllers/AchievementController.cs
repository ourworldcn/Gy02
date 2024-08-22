using AutoMapper;
using GY02;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Managers;
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
        public AchievementController(GameAccountStoreManager accountStore, IMapper mapper, SyncCommandManager syncCommandManager,
            GameTemplateManager templateManager, GameAchievementManager achievementManager, GameSearcherManager gameSearcherManager)
        {
            _AccountStore = accountStore;
            _Mapper = mapper;
            _SyncCommandManager = syncCommandManager;
            _TemplateManager = templateManager;
            _AchievementManager = achievementManager;
            _GameSearcherManager = gameSearcherManager;
        }

        private GameAccountStoreManager _AccountStore;

        IMapper _Mapper;

        SyncCommandManager _SyncCommandManager;

        GameTemplateManager _TemplateManager;
        GameAchievementManager _AchievementManager;
        GameSearcherManager _GameSearcherManager;

#if DEBUG
        /// <summary>
        /// 获取一个样例。
        /// </summary>
        /// <param name="achievementManager"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<TemplateStringFullView> GetTemplateDemo([FromServices] GameAchievementManager achievementManager)
        {
            var tt = achievementManager.GetTemplateById(new Guid("43E9286A-904C-4923-B477-482C0D6470A5"));
            return tt;
        }
#endif

        /// <summary>
        /// 按指定的页签返回一组任务/成就的状态。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="blueprintManager"></param>
        /// <param name="entityManager"></param>
        /// <returns></returns>
        /// <response code="401">令牌无效。</response>  
        [HttpPost]
        public ActionResult<GetAchievementStateWithGenusReturnDto> GetAchievementStateWithGenus(GetAchievementStateWithGenusParamsDto model,
            [FromServices] GameBlueprintManager blueprintManager, [FromServices] GameEntityManager entityManager)
        {
            var result = new GetAchievementStateWithGenusReturnDto { };
            using var dw = _AccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new GetAchievementStateWithGenusCommand { GameChar = gc, };
            var now = OwHelper.WorldNow;

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);

            if (!result.HasError)   //若需要后处理
            {
                if (model.OnlyValid)
                    result.Result.RemoveAll(c => !c.IsValid);

                foreach (var item in result.Result)
                {
                    if (!item.IsValid) continue;
                    if (_TemplateManager.GetFullViewFromId(item.TemplateId) is not TemplateStringFullView tt) continue;
                    if (tt.Genus is null || tt.Genus.Length <= 0) continue;
                    if (tt.Genus.Contains("cj_meiri"))  //日任务
                    {
                        item.Start = now.Date;
                        item.End = now.Date + TimeSpan.FromDays(1);
                    }
                    else if (tt.Genus.Contains("cj_zhourenwu"))//周任务
                    {
                        var inItem = tt.Achievement.Ins?[0];
                        if (inItem is null) continue;
                        var cond = inItem.Conditional?[0];
                        if (cond is null) continue;
                        if (cond.NumberCondition is not NumberCondition nc) continue;
                        if (!_GameSearcherManager.GetMatch(entityManager.GetAllEntity(command.GameChar), inItem, 1, out var ge)) continue;    //转轮
                        var current = ge.Count;
                        if (!nc.GetCurrentPeriod(current, out var s, out var e)) continue;
                        item.Start = now.Date - TimeSpan.FromDays((double)(current - s));
                        item.End = item.Start + TimeSpan.FromDays((double)(e - s + 1));
                    }
                }
            }
            return result;
        }

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
