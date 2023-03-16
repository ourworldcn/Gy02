using AutoMapper;
using Gy02.Publisher;
using Gy02Bll.Commands;
using Gy02Bll.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OW.Game.Manager;
using OW.SyncCommand;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Gy02.Controllers
{
    /// <summary>
    /// 账号管理。
    /// </summary>
    public class AccountController : GameControllerBase
    {
        static IPAddress? _LocalIp;
        /// <summary>
        /// 
        /// </summary>
        public IPAddress LocalIp
        {
            get
            {
                if (_LocalIp is null)
                {
                    IPAddress tmp;
                    if (Request.Host.ToString().Contains("localhost", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                        tmp = addressList.Last(c => c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    }
                    else
                        tmp = IPAddress.Parse(Request.Host.Host);
                    Interlocked.CompareExchange(ref _LocalIp, tmp, null);
                }
                return _LocalIp;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public AccountController()
        {
        }
        static GyUdpClient? udp;
        /// <summary>
        /// 测试代码专用。
        /// </summary>
        /// <param name="str">测试参数。</param>
        /// <param name="udpServer"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<string> Test([FromServices] UdpServerManager udpServer, [AllowNull] string str = null)
        {
            if (udp is null)
            {
                var udp = new GyUdpClient();
                udp.Start();
            }
            return udpServer.Port.ToString();
        }

        /// <summary>
        /// 创建一个新账号。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="mapper">注入的AutoMapper服务。</param>
        /// <param name="commandMng">注入的命令处理器服务。</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CreateAccountResultDto> CreateAccount(CreateAccountParamsDto model, [FromServices] IMapper mapper, [FromServices] SyncCommandManager commandMng)
        {
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
        /// <param name="udpServer"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LoginReturnDto> Login(LoginParamsDto model, [FromServices] IMapper mapper, [FromServices] SyncCommandManager commandMng,
            [FromServices] UdpServerManager udpServer)
        {
            var command = mapper.Map<LoginCommand>(model);
            commandMng.Handle(command);
            var result = mapper.Map<LoginReturnDto>(command);
            string ip = LocalIp.ToString();

            var worldServiceHost = $"{Request.Scheme}://{ip}:{Request.Host.Port}";
            var udpServiceHost = $"{ip}:{udpServer.Port}";
            result.WorldServiceHost = worldServiceHost;
            result.UdpServiceHost = udpServiceHost;
            return result;
        }
    }

}
