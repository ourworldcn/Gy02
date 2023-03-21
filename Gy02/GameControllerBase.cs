using AutoMapper;
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
        /// <summary>
        /// 
        /// </summary>
        protected GameControllerBase() : base()
        {
        }

        /// <summary>
        /// 若服务器正在关闭则抛出异常。
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        protected void ThrowIfStopping()
        {
            var lifetime = HttpContext.RequestServices.GetRequiredService<IHostApplicationLifetime>();
            if (lifetime.ApplicationStopping.IsCancellationRequested)
                throw new InvalidOperationException("服务器正在关闭。");
        }
    }
}
