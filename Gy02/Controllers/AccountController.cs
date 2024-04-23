using AutoMapper;
using Gy02.Controllers;
using Gy02.Test;
using GY02.Commands;
using GY02.Commands.Account;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.AspNetCore.Mvc;
using OW.SyncCommand;
using System.Net;
using System.Net.Sockets;

namespace GY02.Controllers
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
                        try
                        {
                            var addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                            tmp = addressList.Last(c => c.AddressFamily == AddressFamily.InterNetwork);
                        }
                        catch (Exception err)
                        {
                            _Logger.LogWarning(err, "获取本机地址出现异常");
                            throw;
                        }
                    }
                    else
                    {
                        try
                        {
                            var host = Dns.GetHostAddresses(Request.Host.Host);

                            tmp = IPAddress.Parse(host[0].ToString());
                        }
                        catch
                        {
                            throw new InvalidOperationException($"不认识的Ip格式,Ip={Request.Host.Host}");
                        }
                    }
                    Interlocked.CompareExchange(ref _LocalIp, tmp, null);
                }
                return _LocalIp;
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="gameAccountStore"></param>
        /// <param name="syncCommandManager"></param>
        /// <param name="mapper"></param>
        /// <param name="logger"></param>
        public AccountController(GameAccountStoreManager gameAccountStore, SyncCommandManager syncCommandManager, IMapper mapper, ILogger<AccountController> logger)
        {
            _GameAccountStore = gameAccountStore;
            _SyncCommandManager = syncCommandManager;
            _Mapper = mapper;
            _Logger = logger;
        }

        readonly GameAccountStoreManager _GameAccountStore;
        readonly SyncCommandManager _SyncCommandManager;
        readonly IMapper _Mapper;
        ILogger<AccountController> _Logger;

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

#if DEBUG
        TestUdpServerManager testUdpServerManager;
#endif

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
            string ip;
            try
            {
                ip = LocalIp.ToString();
            }
            catch (Exception err)
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = err.Message;
                _Logger.LogWarning(err.Message);
                return result;
            }
            try
            {
                var worldServiceHost = $"{Request.Scheme}://{ip}:{Request.Host.Port}";
                var udpServiceHost = $"{ip}:{((IPEndPoint)udpServer.ListernEndPoint).Port}";
                result.WorldServiceHost = worldServiceHost;
                result.UdpServiceHost = udpServiceHost;
#if DEBUG
                testUdpServerManager = new TestUdpServerManager(HttpContext.RequestServices);
                testUdpServerManager.Test(command.User.Token, IPEndPoint.Parse(udpServiceHost));
#endif
                return result;

            }
            catch (Exception err)
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = err.Message;
                _Logger.LogWarning(err.Message);
                return result;
            }
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
            var result = _Mapper.Map<LoginT78ReturnDto>(command);
            var worldServiceHost = $"{Request.Scheme}://{ip}:{Request.Host.Port}";
            var udpServiceHost = $"{ip}:{udpServer.ListernEndPoint}";
            result.WorldServiceHost = worldServiceHost;
            result.UdpServiceHost = udpServiceHost;
            return result;
        }

        /// <summary>
        /// 特定发行商sdk创建或登录用户。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="udpServer"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LoginT21ReturnDto> LoginT21(LoginT21ParamsDto model, [FromServices] UdpServerManager udpServer)
        {
            //    if (gm.Id2OnlineChar.Count > 10000 * Environment.ProcessorCount)
            //        return StatusCode((int)HttpStatusCode.ServiceUnavailable, "登录人数过多，请稍后登录");
            //    var gu = gm.LoginT21(model.Sid, out string pwd);
            // 完美 北美登录。
            var command = _Mapper.Map<LoginT21Command>(model);
            _SyncCommandManager.Handle(command);

            string ip = LocalIp.ToString();
            var result = _Mapper.Map<LoginT21ReturnDto>(command);
            var worldServiceHost = $"{Request.Scheme}://{ip}:{Request.Host.Port}";
            var udpServiceHost = $"{ip}:{((IPEndPoint)udpServer.ListernEndPoint).Port}";
            result.WorldServiceHost = worldServiceHost;
            result.UdpServiceHost = udpServiceHost;
            return result;
        }

        /// <summary>
        /// T1228合作伙伴登录的接口。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="udpServer"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LoginT1228ReturnDto> LoginT1228(LoginT1228ParamsDto model, [FromServices] UdpServerManager udpServer)
        {
            var command = _Mapper.Map<LoginT1228Command>(model);
            _SyncCommandManager.Handle(command);
            string ip = LocalIp.ToString();
            var result = _Mapper.Map<LoginT1228ReturnDto>(command);
            result.FillErrorFrom(command);
            if (!result.HasError)
            {
                var worldServiceHost = $"{Request.Scheme}://{ip}:{Request.Host.Port}";
                var udpServiceHost = $"{ip}:{((IPEndPoint)udpServer.ListernEndPoint).Port}";
                result.WorldServiceHost = worldServiceHost;
                result.UdpServiceHost = udpServiceHost;
                result.LoginName = command.User.LoginName;
                result.Pwd = command.Pwd;
            }
            return result;
        }

        /// <summary>
        /// 0314合作伙伴登录接口。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="udpServer"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LoginT0314ReturnDto> LoginT0314(LoginT0314ParamsDto model, [FromServices] UdpServerManager udpServer)
        {
            //捷游/东南亚
            var command = _Mapper.Map<LoginT0314Command>(model);
            _SyncCommandManager.Handle(command);
            string ip = LocalIp.ToString();
            var result = _Mapper.Map<LoginT0314ReturnDto>(command);
            result.FillErrorFrom(command);
            if (!result.HasError)
            {
                var worldServiceHost = $"{Request.Scheme}://{ip}:{Request.Host.Port}";
                var udpServiceHost = $"{ip}:{udpServer.ListernEndPoint}";
                result.WorldServiceHost = worldServiceHost;
                result.UdpServiceHost = udpServiceHost;
                result.LoginName = command.User.LoginName;
                result.Pwd = command.Pwd;
            }
            return result;
        }

        /// <summary>
        /// 心跳功能，延迟被驱逐的时间。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
