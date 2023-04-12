using Gy02Bll.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gy02.Controllers
{
    /// <summary>
    /// 战斗控制器。
    /// </summary>
    public class CombatController : GameControllerBase
    {
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
        public CombatController(IServiceProvider service, GameAccountStore gameAccountStore, GameCombatManager gameCombatManager) : base(service)
        {
            _GameAccountStore = gameAccountStore;
            _GameCombatManager = gameCombatManager;
        }
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释

        GameAccountStore _GameAccountStore;
        GameCombatManager _GameCombatManager;
#if DEBUG
        /// <summary>
        /// 测试。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<bool> Test() { return true; }
#endif


    }
}
