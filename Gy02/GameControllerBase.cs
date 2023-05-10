using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using OW.Game.Entity;
using OW.Game.Manager;

namespace GY02
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
        /// 
        /// </summary>
        /// <param name="service"></param>
        protected GameControllerBase(IServiceProvider service)
        {
            _Service = service;
        }

        IServiceProvider? _Service;

        /// <summary>
        /// 获取本范围(Scope)的容器服务。
        /// </summary>
        public IServiceProvider? Service => _Service;

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
