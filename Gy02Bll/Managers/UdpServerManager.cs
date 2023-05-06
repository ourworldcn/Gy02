using Gy02.Publisher;
using Gy02Bll.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gy02Bll.Managers
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

    public class UdpServerManager : BackgroundService
    {
        public UdpServerManager(IHostApplicationLifetime lifetime, ILogger<UdpServerManager> logger, IOptions<UdpServerManagerOptions> options)
        {
            _Lifetime = lifetime;
            _Logger = logger;
            _Options = options.Value;
            var count = Interlocked.Increment(ref _Count);
            if (_Count > 1)
                _Logger.LogWarning($"检测到UdpServerManager第{count}个实例。");
        }
        volatile static int _Count = 0;
        IHostApplicationLifetime _Lifetime;
        ILogger<UdpServerManager> _Logger;
        UdpServerManagerOptions _Options;

        /// <summary>
        /// 听的端口号。
        /// </summary>
        public int ListenerPort => ((IPEndPoint)_Udp.Client.LocalEndPoint).Port;

        volatile UdpClient _Udp;

        /// <summary>
        /// 用户令牌对应的远程客户端地址端口。
        /// </summary>
        static ConcurrentDictionary<Guid, IPEndPoint> _Token2EndPoint = new ConcurrentDictionary<Guid, IPEndPoint>();

        static BlockingCollection<(Guid, byte[], int)> _Queue = new BlockingCollection<(Guid, byte[], int)>();

        public static Guid PingGuid = Guid.Parse("{D99A07D0-DF3E-43F7-8060-4C7140905A29}");

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _Udp = new UdpClient(_Options.LocalPort);
            _Logger.LogDebug($"UdpServer开始侦听{_Udp.Client.LocalEndPoint}。");

            Task.Factory.StartNew(ListernCallback, TaskCreationOptions.LongRunning);
            return Task.Factory.StartNew(WriteCallback, TaskCreationOptions.LongRunning);
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
        public void Send(Guid token, byte[] data)
        {
            _Queue.Add((token, data, 4));
        }

        /// <summary>
        /// 写入线程。
        /// </summary>
        void WriteCallback()
        {
            while (!_Lifetime.ApplicationStopping.IsCancellationRequested)
            {
                try
                {
                    var tmp = _Queue.Take(_Lifetime.ApplicationStopping);
                    using var dw = DisposeHelper.Create(c => ArrayPool<byte>.Shared.Return(c), tmp.Item2);  //回收
                    if (_Token2EndPoint.TryGetValue(tmp.Item1, out var ip))
                    {
                        _Udp.Send(tmp.Item2, tmp.Item2.Length, ip);
                        _Logger.LogWarning($"发送信息{_Udp.Client.LocalEndPoint} -> {ip} : {tmp.Item2.Length}字节");
                    }
                }
                catch (InvalidOperationException)   //基础集合在此实例之外 BlockingCollection<T> 进行了修改，或 BlockingCollection<T> 为空，并且已标记为已完成，并已对添加内容进行标记。
                {
                    //不可能发生
                    throw;
                }
                catch (OperationCanceledException)  //CancellationToken 已取消
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 侦听线程。
        /// </summary>
        void ListernCallback()
        {
            while (!_Lifetime.ApplicationStopping.IsCancellationRequested)
            {
                UdpReceiveResult result = default;
                try
                {
                    result = _Udp.ReceiveAsync(_Lifetime.ApplicationStopping).AsTask().Result;
                    _Logger.LogTrace($"收到信息{result.Buffer.Length}字节。");
                }
                catch (AggregateException excp) when (excp.InnerException is TaskCanceledException) //若应用已经试图推出
                {
                    break;
                }
                catch (ObjectDisposedException)  //已关闭基础 Socket。
                { }
                catch (SocketException)  //访问套接字时出错。
                {

                }
                try
                {
                    var token = new Guid(result.Buffer);
                    if (token == PingGuid)
                    {
                        var count = _Udp.Send(new byte[] { 12, 34 }, 2, result.RemoteEndPoint);
                        _Logger.LogWarning($"回应了{count}字节，ip={result.RemoteEndPoint}");
                    }
                    else
                    {
                        _Token2EndPoint.AddOrUpdate(token, result.RemoteEndPoint, (t, p) => result.RemoteEndPoint);
                        SendObject(token, new ListenStartedDto() { Token = token, IPEndpoint = result.RemoteEndPoint.ToString() });  //发送确认
                    }
                }
                catch (Exception excp)
                {
                    _Logger.LogWarning(excp, "回应信息时出错。");
                }
            }
        }
    }

    public static class UdpServerManagerExtensions
    {
        public static IServiceCollection AddUdpServerManager(this IServiceCollection services)
        {
            services.AddHostedService<UdpServerManager>();
            services.AddSingleton(c => (UdpServerManager)c.GetServices<IHostedService>().First(c => c is UdpServerManager));
            return services;
        }
    }
}
