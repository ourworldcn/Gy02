using Microsoft.AspNetCore.Mvc;
using OW.Game.Entity;
using OW.Game.Manager;

namespace Gy02
{
    /// <summary>
    /// 游戏控制器基类。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GameControllerBase : ControllerBase
    {
    }
}
