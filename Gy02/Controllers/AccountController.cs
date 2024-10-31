using AutoMapper;
using Gy02.Controllers;
using Gy02.Test;
using GY02.Commands;
using GY02.Commands.Account;
using GY02.Managers;
using GY02.Publisher;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using OW.Game.Entity;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.SyncCommand;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

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
        /// <param name="udpServer"></param>
        /// <param name="httpClientFactory"></param>
        public AccountController(GameAccountStoreManager gameAccountStore, SyncCommandManager syncCommandManager, IMapper mapper, ILogger<AccountController> logger,
            UdpServerManager udpServer, IHttpClientFactory httpClientFactory)
        {
            _GameAccountStore = gameAccountStore;
            _SyncCommandManager = syncCommandManager;
            _Mapper = mapper;
            _Logger = logger;
            _UdpServer = udpServer;
            _HttpClientFactory = httpClientFactory;
        }

        readonly GameAccountStoreManager _GameAccountStore;
        readonly SyncCommandManager _SyncCommandManager;
        readonly IMapper _Mapper;
        ILogger<AccountController> _Logger;
        UdpServerManager _UdpServer;
        IHttpClientFactory _HttpClientFactory;

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
#if DEBUG
            var tmp = new List<GamePropertyChangeItem<object>>() { new GamePropertyChangeItem<object>() };
            var gamePropertyChanges = mapper.Map<List<GamePropertyChangeItemDto>>(tmp);
#endif 
            var command = mapper.Map<CreateAccountCommand>(model);
            commandMng.Handle(command);
            var result = mapper.Map<CreateAccountResultDto>(command);
            return result;
        }

#if DEBUG
        TestUdpServerManager? testUdpServerManager;
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
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LoginT78ReturnDto> LoginT78(LoginT78ParamsDto model)
        {
            //    if (gm.Id2OnlineChar.Count > 10000 * Environment.ProcessorCount)
            //        return StatusCode((int)HttpStatusCode.ServiceUnavailable, "登录人数过多，请稍后登录");
            //    var gu = gm.LoginT78(model.Sid, out string pwd);

            var command = _Mapper.Map<LoginT78Command>(model);
            _SyncCommandManager.Handle(command);

            string ip = LocalIp.ToString();
            var result = _Mapper.Map<LoginT78ReturnDto>(command);
            var worldServiceHost = $"{Request.Scheme}://{ip}:{Request.Host.Port}";
            var udpServiceHost = $"{ip}:{((IPEndPoint)_UdpServer.ListernEndPoint).Port}";
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
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LoginT0314TapTapReturnDto> LoginT0314TapTap(LoginT0314TapTapParamsDto model)
        {
            //捷游/东南亚TapTap
            var result = new LoginT0314TapTapReturnDto { };
            using var dw = _GameAccountStore.GetOrLoadUser(model.Uid, model.Uid, out var gu);
            var isCreate = false;
            if (dw.IsEmpty)  //若没有创建
            {
                var commandCreate = new CreateAccountCommand { LoginName = model.Uid, Pwd = model.Uid };
                _SyncCommandManager.Handle(commandCreate);
                if (commandCreate.HasError)
                {
                    result.FillErrorFrom(commandCreate);
                    return result;
                }
                isCreate = true;
            }

            var command = new LoginCommand
            {
                LoginName = model.Uid,
                Pwd = model.Uid,
            };
            _SyncCommandManager.Handle(command);
            if (command.HasError)
            {
                result.FillErrorFrom(command);
                return result;
            }
            result = _Mapper.Map<LoginT0314TapTapReturnDto>(command);
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
                var udpServiceHost = $"{ip}:{((IPEndPoint)_UdpServer.ListernEndPoint).Port}";
                result.WorldServiceHost = worldServiceHost;
                result.UdpServiceHost = udpServiceHost;
                if (!isCreate) result.Pwd = null;
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
        /// 304合作伙伴登录接口V2。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="udpServer"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LoginT304V2ReturnDto> LoginT304V2(LoginT304V2ParamsDto model, [FromServices] UdpServerManager udpServer)
        {
            //完美/北美
            var result = new LoginT304V2ReturnDto { };
            using var dw = _GameAccountStore.GetOrLoadUser(model.Uid, model.Uid, out var gu);
            var isCreate = false;
            if (dw.IsEmpty)  //若没有创建
            {
                var commandCreate = new CreateAccountCommand { LoginName = model.Uid, Pwd = model.Uid };
                _SyncCommandManager.Handle(commandCreate);
                if (commandCreate.HasError)
                {
                    result.FillErrorFrom(commandCreate);
                    return result;
                }
                isCreate = true;
            }

            var command = new LoginCommand
            {
                LoginName = model.Uid,
                Pwd = model.Uid,
            };
            _SyncCommandManager.Handle(command);
            if (command.HasError)
            {
                result.FillErrorFrom(command);
                return result;
            }
            result = _Mapper.Map<LoginT304V2ReturnDto>(command);
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
                if (!isCreate) result.Pwd = null;
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
        /// 心跳功能，延迟被驱逐的时间。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">令牌无效。</response>  
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

        /// <summary>
        /// 统一登陆接口。
        /// 验证方式:T1021/NA,需要证据：Uid，Token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LoginV2ReturnDto> LoginV2(LoginV2ParamsDto model)
        {
            var result = new LoginV2ReturnDto();
            var command = new LoginCommand();
            var isCreate = false;   //是否是新创建
            var isCreateSucc = false;
            string errMsg ;
            var errCode = ErrorCodes.NO_ERROR;

            switch (model.Mode)
            {
                case "T1021/NA":
                case "T1021/EU":
                    {
                        //欧美发型/欧美地区
                        if (!model.Evidence.TryGetValue("Uid", out var uid))   //若无法得到用户名
                        {
                            result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                            result.DebugMessage = "缺少证据项Uid。";
                            result.HasError = true;
                            return result;
                        }
                        if (!model.Evidence.TryGetValue("Token", out var token))   //若无法得到令牌
                        {
                            result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                            result.DebugMessage = "缺少证据项Token。";
                            result.HasError = true;
                            return result;
                        }
                        //string url = "https://sdk-lmzha.lmzhom66.com/webapi/checkUserInfo";  //token认证地址
                        string url = "https://sdkapi.difeh.com/webapi/checkUserInfo";  //token认证地址
                        var httpClient = _HttpClientFactory.CreateClient("T1021/NA");
                        var p = new T1021NALoginParamsSdkDto { uid = uid, token = token };

                        var dic = new Dictionary<string,string>();
                        dic.Add("uid", uid);
                        dic.Add("token", token);
                        
                        using var respone = httpClient.PostAsync(url, new FormUrlEncodedContent(dic)).Result; 

                        if (!respone.IsSuccessStatusCode)   //若不成功
                        {
                            result.ErrorCode = ErrorCodes.ERROR_INVALID_DATA;
                            result.DebugMessage = $"调用验证不成功,返回码{respone.StatusCode}。";
                            result.HasError = true;
                            return result;
                        }
                        var str1 = respone.Content.ReadAsStringAsync().Result;
                        var obj = JsonSerializer.Deserialize<T1021NALoginReturnSdkDto>(str1)!;
                        if (!obj.status)
                        {
                            result.ErrorCode = ErrorCodes.ERROR_LOGON_FAILURE;
                            result.DebugMessage = obj.message;
                            result.HasError = true;
                            return result;
                        }

                        using var dw = _GameAccountStore.GetOrLoadUser(uid, uid, out var gu);
                        if (dw.IsEmpty)  //若没有创建
                        {
                            var commandCreate = new CreateAccountCommand { LoginName = uid, Pwd = uid };
                            _SyncCommandManager.Handle(commandCreate);
                            if (commandCreate.HasError)
                            {
                                result.FillErrorFrom(commandCreate);
                                return result;
                            }
                            isCreate = true;
                        }

                        command.LoginName = uid;
                        command.Pwd = uid;
                        _SyncCommandManager.Handle(command);
                        if (command.HasError)
                        {
                            result.FillErrorFrom(command);
                            return result;
                        }
                        else
                            isCreateSucc = true;

                        result.Token = command.User.Token;
                        result.GameChar = _Mapper.Map<GameCharDto>(command.User.CurrentChar);
                        result.Pwd = isCreate ? command.Pwd : null;
                        result.UserId = command.User.Id;
                        string ip;

                        try
                        {
                            ip = LocalIp.ToString();
                        }
                        catch (Exception err)
                        {
                            errCode= ErrorCodes.ERROR_BAD_ARGUMENTS;
                            errMsg = err.Message;
                            goto lbErr;
                        }
                        try
                        {
                            var worldServiceHost = $"{Request.Scheme}://{ip}:{Request.Host.Port}";
                            var udpServiceHost = $"{ip}:{((IPEndPoint)_UdpServer.ListernEndPoint).Port}";
                            result.WorldServiceHost = worldServiceHost;
                            result.UdpServiceHost = udpServiceHost;
                            if (!isCreate) result.Pwd = null;
                            return result;
                        }
                        catch (Exception err)
                        {
                            result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                            result.DebugMessage = err.Message;
                            _Logger.LogWarning(err.Message);
                        }
                    }
                    break;
                default:
                    break;
            }
            return result;
        lbErr:
            if (errCode != ErrorCodes.NO_ERROR)
            {
                result.HasError = true;
                result.ErrorCode = errCode;
                result.DebugMessage = errMsg;
                _Logger.LogWarning(errMsg);
            }
            return result;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class T1021NALoginReturnSdkDto
    {
        /// <summary>
        /// 接口验证状态，若通过验证为true，否则为false。
        /// </summary>
        [JsonPropertyName("status")]
        public bool status { get; set; }

        /// <summary>
        /// 玩家账号ID（自主平台的该值和客户端一致）。
        /// </summary>
        [JsonPropertyName("uid")]
        public string uid { get; set; }

        /// <summary>
        /// status 为false 时，message 有值，为错误提示语。
        /// </summary>
        [JsonPropertyName("message")]
        public string message { get; set; }

        /// <summary>
        /// 如果status 为true 时，data 数组包含了用户账号绑定信息。
        /// </summary>
        [JsonPropertyName("data")]
        public object data { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class T1021NALoginParamsSdkDto
    {
        /// <summary>
        /// 从客户端登录回调中获取的uid。
        /// </summary>
        [JsonPropertyName("uid")]
        public string? uid { get; set; }

        /// <summary>
        /// 从客户端登录回调中获取的token，请注意token的长度不要被截断。
        /// </summary>
        [JsonPropertyName("token")]
        public string? token { get; set; }
    }
}
