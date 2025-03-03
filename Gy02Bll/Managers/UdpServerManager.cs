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
using System.Text;
using System.Text.Json;

namespace GY02.Managers
{
    public class UdpServerManagerOptions : IOptions<UdpServerManagerOptions>
    {
        //public override UdpServerManagerOptions Value => this;

        /// <summary>
        /// 侦听地址。
        /// 指定使用的本地终结点Ip,通常不用设置。
        /// </summary>
        /// <value>默认侦听虚四段表示法中的 0.0.0.0。</value>
        public string LocalIp { get; set; } = "0.0.0.0";

        /// <summary>
        /// 使用的本机侦听端口。应通过配置指定端口，避免防火墙拒绝侦听请求。
        /// </summary>
        /// <value>默认值：0,自动选择。</value>
        public ushort LocalPort { get; set; }

        public UdpServerManagerOptions Value => this;
    }

    /// <summary>
    /// 游戏服务器的Udp管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class UdpServerManager : OwRdmServer
    {
        #region 静态成员

        /// <summary>
        /// 用户令牌对应的远程客户端地址端口。
        /// </summary>
        static ConcurrentDictionary<Guid, int> _Token2ClientId = new ConcurrentDictionary<Guid, int>();

        #endregion 静态成员

        #region 属性及相关

        IHostApplicationLifetime _Lifetime;
        GameAccountStoreManager _AccountStoreManager;

        Timer _Timer;

        #endregion 属性及相关

        #region 构造函数

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public UdpServerManager(IOptions<UdpServerManagerOptions> options, ILogger<UdpServerManager> logger, IHostApplicationLifetime lifetime, GameAccountStoreManager accountStoreManager)
            : base(new OwRdmServerOptions { ListernPort = options.Value.LocalPort, ListernAddress = options.Value.LocalIp }, logger, lifetime)
        {
            _Lifetime = lifetime;
            _AccountStoreManager = accountStoreManager;
            _Timer = new Timer(ClearToken, null, 60_000, 60_000);
            _Lifetime.ApplicationStopping.Register(() => Stopping.Cancel());    //试图关闭清理计时器
            Logger.LogInformation("UdpServer开始侦听{LocalEndPoint}。", ListernEndPoint);
        }

        #endregion 构造函数

        #region 方法

        /// <summary>
        /// 获取客户端名称。
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        string GetClientName(ReadOnlySpan<byte> buffer)
        {
            return Encoding.UTF8.GetString(buffer);
        }

        protected override void OnRequestConnect(OwRdmDgram datas, EndPoint remote)
        {
            var clientName = GetClientName(new Span<byte>(datas.Buffer, datas.Offset + 8, datas.Count - 8)); //获取客户端传来的名称
            if (!Guid.TryParse(clientName, out var token)) goto goon;
            Logger.LogDebug("检测到连接请求，Ip={ip},Id={id}", remote, token);
            _Token2ClientId.AddOrUpdate(token, c => datas.Id, (c, d) => datas.Id);
        goon:
            base.OnRequestConnect(datas, remote);
        }

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
            if (!_Token2ClientId.TryGetValue(token, out var id))    //若未找到指定的ip地址
                return false;
            SendTo(data, 0, data.Length, id);
            return true;
        }

        /// <summary>
        /// 清理过时的Token。
        /// </summary>
        void ClearToken(object state)
        {
            foreach (var item in _Token2ClientId)
            {
                if (!_AccountStoreManager.Token2Key.ContainsKey(item.Key))   //若不存在指定的Token
                {
                    if (_Token2ClientId.Remove(item.Key, out var id))
                    {
                        if (RemoveId(id, out _))
                            Logger?.LogDebug("成功移除了udp连接信息，Token = {token}，Id = {id}", item.Key, id);
                    }
                }
            }
        }

        #endregion 方法

        #region IDisposable接口及相关

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {

                }
            }
            base.Dispose(disposing);
        }

        #endregion IDisposable接口及相关

        public class AccountLogoutingHandler : SyncCommandHandlerBase<AccountLogoutingCommand>
        {
            public override void Handle(AccountLogoutingCommand command)
            {
                _Token2ClientId.TryRemove(command.User.Token, out _);
            }
        }
    }

}
