using AutoMapper;
using Gy02.Publisher;
using Gy02Bll.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OW.Game;
using OW.Game.Manager;
using System.Collections.Generic;
using System.Diagnostics;

namespace Gy02.Controllers
{
    /// <summary>
    /// 账号管理。
    /// </summary>
    public class AccountController : GameControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        public AccountController()
        {
        }

        /// <summary>
        /// 测试代码专用。
        /// </summary>
        /// <param name="str">测试参数。</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<bool> Test(string str)
        {
            return true;
        }

        /// <summary>
        /// 创建一个新账号。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="mapper">注入的AutoMapper服务。</param>
        /// <param name="commandMng">注入的命令处理器服务。</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CreateAccountResultDto> CreateAccount(CreateAccountParamsDto model, [FromServices] IMapper mapper, [FromServices] GameCommandManager commandMng)
        {
            //var service = HttpContext.RequestServices.GetRequiredService<IServiceProvider>();
            var command = mapper.Map<CreateAccountCommand>(model);
            commandMng.Handle(command);
            var result = mapper.Map<CreateAccountResultDto>(command);
            return result;
        }

        /// <summary>
        /// 登录账号。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="mapper"></param>
        /// <param name="commandMng"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LoginReturnDto> Login(LoginParamsDto model, [FromServices] IMapper mapper, [FromServices] GameCommandManager commandMng)
        {
            var command = mapper.Map<LoginCommand>(model);
            commandMng.Handle(command);
            var result = mapper.Map<LoginReturnDto>(command);
            return result;
        }
    }

}
