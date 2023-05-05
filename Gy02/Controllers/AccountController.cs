using AutoMapper;
using GuangYuan.GY001.BLL;
using Gy02.Publisher;
using Gy02Bll.Commands;
using Gy02Bll.Commands.Account;
using Gy02Bll.Commands.Combat;
using Gy02Bll.Managers;
using Gy02Bll.Templates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.PropertyChange;
using OW.Game.Store;
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
        public AccountController(GameAccountStore gameAccountStore, SyncCommandManager syncCommandManager, IMapper mapper)
        {
            _GameAccountStore = gameAccountStore;
            _SyncCommandManager = syncCommandManager;
            _Mapper = mapper;
        }

        GameAccountStore _GameAccountStore;
        SyncCommandManager _SyncCommandManager;
        IMapper _Mapper;
#if DEBUG

        /// <summary>
        /// 测试代码专用。
        /// </summary>
        /// <param name="udpServer">测试参数。</param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<List<CostInfo>> Test([FromServices] UdpServerManager udpServer, [FromServices] IMapper mapper)
        {
            var src = new GamePropertyChangeItem<object>() { Object = new GameItem(new VirtualThing() { ExtraGuid = Guid.NewGuid() }) };
            var dest = mapper.Map<GamePropertyChangeItemDto>(src);
            return new List<CostInfo>();
        }
#endif

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
            var udpServiceHost = $"{ip}:{udpServer.ListenerPort}";
            result.WorldServiceHost = worldServiceHost;
            result.UdpServiceHost = udpServiceHost;
#if DEBUG
            GyUdpClient udp = new GyUdpClient();
            var serverIp = IPEndPoint.Parse(result.UdpServiceHost);
            udp.Start(result.Token, serverIp);
#endif
            return result;
        }

        /// <summary>
        /// 特定发行商sdk创建或登录用户。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="udpServer"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LoginT78ReturnDto> LoginT78(LoginT78ParamsDto model, [FromServices] UdpServerManager udpServer)
        {
            //    if (gm.Id2OnlineChar.Count > 10000 * Environment.ProcessorCount)
            //        return StatusCode((int)HttpStatusCode.ServiceUnavailable, "登录人数过多，请稍后登录");
            //    var gu = gm.LoginT78(model.Sid, out string pwd);

            var command = _Mapper.Map<LoginT78Command>(model);
            _SyncCommandManager.Handle(command);

            string ip = LocalIp.ToString();
            var result = _Mapper.Map<LoginT78ReturnDto> (command);
            var worldServiceHost = $"{Request.Scheme}://{ip}:{Request.Host.Port}";
            var udpServiceHost = $"{ip}:{udpServer.ListenerPort}";
            result.WorldServiceHost = worldServiceHost;
            result.UdpServiceHost = udpServiceHost;
            return result;
        }

        /// <summary>
        /// 心跳功能，延迟被驱逐的时间。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<NopReturnDto> Nop(NopParamsDto model)
        {
            var result = new NopReturnDto { };
            using var dw = _GameAccountStore.GetCharFromToken(model.Token, out var gc);
            if (dw.IsEmpty)
            {
                if (OwHelper.GetLastError() == ErrorCodes.ERROR_INVALID_TOKEN) return Unauthorized();
                result.FillErrorFromWorld();
                return result;
            }

            var command = new NopCommand { GameChar = gc, };

            _Mapper.Map(model, command);
            _SyncCommandManager.Handle(command);
            _Mapper.Map(command, result);
            return result;
        }
    }

}
