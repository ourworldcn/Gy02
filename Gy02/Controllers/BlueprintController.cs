using AutoMapper;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.Templates;
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
        /// <param name="blueprintManager"></param>
        /// <param name="syncCommandManager"></param>
        /// <param name="mapper"></param>
        /// <param name="diceManager"></param>
        /// <param name="accountStoreManager"></param>
        public BlueprintController(GameBlueprintManager blueprintManager, SyncCommandManager syncCommandManager, IMapper mapper, GameDiceManager diceManager, GameAccountStoreManager accountStoreManager)
        {
            _BlueprintManager = blueprintManager;
            _SyncCommandManager = syncCommandManager;
            _Mapper = mapper;
            _DiceManager = diceManager;
            _AccountStoreManager = accountStoreManager;
        }

        private GameBlueprintManager _BlueprintManager;
        private SyncCommandManager _SyncCommandManager;
        private IMapper _Mapper;
        private GameDiceManager _DiceManager;
        private GameAccountStoreManager _AccountStoreManager;

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

        /// <summary>
        /// 获取卡池再抽多少次会得到高价值物品。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">令牌无效。</response>  
        [HttpPost]
        public ActionResult<GetDiceGuaranteesReturnDto> GetDiceGuarantees(GetDiceGuaranteesParamsDto model)
        {
            var result = new GetDiceGuaranteesReturnDto { };
            using var dw = _AccountStoreManager.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var fullView = _DiceManager.GetDiceById(model.DiceTid);
            if (fullView is null)
            {
                result.FillErrorFromWorld();
                return result;
            }
            var i = _DiceManager.GetGuaranteesCount(fullView);
            if (i.HasValue)
            {
                var item = _DiceManager.GetOrAddHistory(fullView, gc);
                result.Count = i.Value - item.GuaranteesCount;
            }
            else
                result.Count = i;
            result.MaxGuaranteesCount = i;
            return result;
        }
    }

}
