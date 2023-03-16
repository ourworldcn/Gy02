using Gy02.Publisher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    public class UdpServerManager : BackgroundService
    {
        public UdpServerManager(IHostApplicationLifetime lifetime, ILogger<UdpServerManager> logger)
        {
            _Lifetime = lifetime;
            _Logger = logger;
        }

        IHostApplicationLifetime _Lifetime;
        ILogger<UdpServerManager> _Logger;

        /// <summary>
        /// 听的端口号。
        /// </summary>
        public int Port => ((IPEndPoint)_UdpListen.Client.LocalEndPoint).Port;

        UdpClient _UdpListen;
        UdpClient _UdpSend = new UdpClient(0);

        ConcurrentDictionary<Guid, IPEndPoint> _Token2EndPoint = new ConcurrentDictionary<Guid, IPEndPoint>();

        BlockingCollection<(Guid, byte[], int)> _Queue = new BlockingCollection<(Guid, byte[], int)>();
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _UdpListen = new UdpClient(0);
            Task.Factory.StartNew(WriteCallback, TaskCreationOptions.LongRunning);
            return Task.Factory.StartNew(() =>
            {
                while (!_Lifetime.ApplicationStopping.IsCancellationRequested)
                {
                    UdpReceiveResult result;
                    try
                    {
                        result = _UdpListen.ReceiveAsync(_Lifetime.ApplicationStopping).AsTask().Result;
                        _Logger.LogWarning($"收到信息{result.Buffer.Length}字节。");
                    }
                    catch (AggregateException excp) when (excp.InnerException is TaskCanceledException) //若应用已经试图推出
                    {
                        break;
                    }
                    try
                    {
                        var token = new Guid(result.Buffer);
                        _Token2EndPoint.AddOrUpdate(token, result.RemoteEndPoint, (t, p) => result.RemoteEndPoint);
                        //SendObject(token, new ListenStartedDto() { Token = token });  //发送确认
                    }
                    catch (Exception excp)
                    {
                        _Logger.LogWarning(excp, "回应信息时出错。");
                    }
                }
                _UdpListen.Close();
            }, TaskCreationOptions.LongRunning);

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
                        _UdpSend.Send(tmp.Item2, tmp.Item2.Length, ip);
                        _Logger.LogWarning($"发送信息{tmp.Item2.Length}字节。");
                    }
                }
                catch (InvalidOperationException)   //基础集合在此实例之外 BlockingCollection<T> 进行了修改，或 BlockingCollection<T> 为空，并且已标记为已完成，并已对添加内容进行标记。
                {
                    break;
                }
                catch (OperationCanceledException)  //CancellationToken 已取消
                {
                    break;
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
