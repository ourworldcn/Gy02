using GY02.Commands;
using GY02.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.DDD;
using OW.Game.Managers;
using OW.SyncCommand;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace GY02.Managers
{
    public class UdpServerManagerOptions : IOptions<UdpServerManagerOptions>
    {
        public UdpServerManagerOptions Value => this;

        /// <summary>
        /// 使用的本机侦听端口。
        /// </summary>
        /// <value>默认值：0,自动选择。应通过配置指定端口，避免防火墙拒绝侦听请求。</value>
        public short LocalPort { get; set; }

        /// <summary>
        /// 指定使用的本地终结点Ip,通常不用设置。
        /// </summary>
        public string LocalIp { get; set; }
    }

    /// <summary>
    /// 游戏服务器的Udp管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class UdpServerManager : GameManagerBase<UdpServerManagerOptions, UdpServerManager>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public UdpServerManager(IOptions<UdpServerManagerOptions> options, ILogger<UdpServerManager> logger, IHostApplicationLifetime lifetime, GameAccountStoreManager accountStoreManager)
            : base(options, logger)
        {
            _Lifetime = lifetime;
            _AccountStoreManager = accountStoreManager;
            _Timer = new Timer(ClearToken, null, 60_000, 60_000);
            _Lifetime.ApplicationStopping.Register(() => _Timer?.Dispose());    //试图关闭清理计时器
            var udpOpt = new OwUdpClientOptions
            {
                LocalEndPoint = new IPEndPoint(IPAddress.Any, Options.LocalPort),
                RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0),
                ReceiveBufferSize = 1024_000,
                SendBufferSize = 1024_000,
                RequestStop = _Lifetime.ApplicationStopping,
                SendPerSeconds = 100,
            };
            _Udp = new OwUdpClient(udpOpt);
            _Udp.UdpDataRecived += _Udp_UdpDataRecived;
            Logger.LogInformation("UdpServer开始侦听{LocalEndPoint}。", udpOpt.LocalEndPoint);
        }


        private void _Udp_UdpDataRecived(object sender, UdpDataRecivedEventArgs e)
        {
            var token = new Guid(e.Data);

            if (_Token2EndPoint.TryGetValue(token, out var oldIpPoint) && !Equals(oldIpPoint, e.RemotePoint)) //若更改ip地址
            {
                Logger.LogWarning("检测到令牌 {token} 将客户端地址从 {oldIpEndPoint} 变更为 {newIpEndPoint}", token, oldIpPoint, e.RemotePoint);
            }
            _Token2EndPoint.AddOrUpdate(token, e.RemotePoint, (t, p) => e.RemotePoint);
            SendObject(token, new ListenStartedDto() { Token = token, IPEndpoint = e.RemotePoint.ToString() });  //发送确认
        }

        IHostApplicationLifetime _Lifetime;
        GameAccountStoreManager _AccountStoreManager;

        Timer _Timer;

        /// <summary>
        /// 听的端口号。
        /// </summary>
        public int ListenerPort => _Udp.LocalEndPoint.Port;

        OwUdpClient _Udp;

        /// <summary>
        /// 用户令牌对应的远程客户端地址端口。
        /// </summary>
        static ConcurrentDictionary<Guid, IPEndPoint> _Token2EndPoint = new ConcurrentDictionary<Guid, IPEndPoint>();

        public static Guid PingGuid = Guid.Parse("{D99A07D0-DF3E-43F7-8060-4C7140905A29}");

        /// <summary>
        /// 发送一个类型。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="obj"></param>
        public void SendObject(Guid token, object obj)
        {
            var type = obj.GetType();
            var guid = type.GUID;
            MemoryStream ms;
            using (ms = new MemoryStream())
            {

                ms.Write(guid.ToByteArray(), 0, 16);
                JsonSerializer.Serialize(ms, obj, type);
            }
            Send(token, ms.ToArray());
        }

        /// <summary>
        /// 发送数据。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="data"></param>
        public bool Send(Guid token, byte[] data)
        {
            if (!_Token2EndPoint.TryGetValue(token, out var ip))    //若未找到指定的ip地址
                return false;
            _Udp.Send(data, ip);
            Logger.LogDebug("发送信息{_Udp.Client.LocalEndPoint} -> {ip} : {tmp.Item2.Length}字节", _Udp.LocalEndPoint, ip, data.Length);
            return true;
        }

        /// <summary>
        /// 清理过时的Token。
        /// </summary>
        void ClearToken(object state)
        {
            foreach (var item in _Token2EndPoint)
            {
                if (!_AccountStoreManager.Token2Key.ContainsKey(item.Key))   //若不存在指定的Token
                    _Token2EndPoint.Remove(item.Key, out _);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_Udp is not null)
            {
                _Udp.Dispose();
            }
            base.Dispose(disposing);
        }

        public class AccountLogoutingHandler : SyncCommandHandlerBase<AccountLogoutingCommand>
        {
            public override void Handle(AccountLogoutingCommand command)
            {
                _Token2EndPoint.TryRemove(command.User.Token, out _);
            }
        }
    }
}
