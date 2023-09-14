using Microsoft.Extensions.Options;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

#if !NETCOREAPP //若非NetCore程序

namespace Microsoft.Extensions.Options
{
    //
    // 摘要:
    //     Used to retrieve configured TOptions instances.
    //
    // 类型参数:
    //   TOptions:
    //     The type of options being requested.
    public interface IOptions</*[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]*/ out TOptions> where TOptions : class
    {
        //
        // 摘要:
        //     The default configured TOptions instance
        TOptions Value { get; }
    }
}
#endif


namespace System.Net.Sockets
{
    /// <summary>
    /// Udp的配置数据类。
    /// </summary>
    public class OwUdpClientOptions : IOptions<OwUdpClientOptions>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwUdpClientOptions()
        {

        }

        /// <summary>
        /// 本地使用的终结点。
        /// </summary>
        /// <value>默认值：new IPEndPoint(IPAddress.Any, 0)</value>
        public IPEndPoint LocalEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 0);

        /// <summary>
        /// 侦听远程的终结点。
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// 发送缓冲区的大小，以字节为单位。
        /// </summary>
        /// <value>默认值：1MByte</value>
        public int SendBufferSize { get; set; } = 1024 * 1024;

        /// <summary>
        /// 接收缓冲区的大小，以字节为单位。
        /// </summary>
        /// <value>默认值：1MByte</value>
        public int ReceiveBufferSize { get; set; } = 1024 * 1024;

        /// <summary>
        /// 请求关闭的同步信号。
        /// </summary>
        /// <value>默认值：<seealso cref="CancellationToken.None"/></value>
        public CancellationToken RequestStop { get; set; } = CancellationToken.None;

        /// <summary>
        /// 每秒发送多少个次数据。不精确，仅是大致的数字。
        /// </summary>
        public int SendPerSeconds { get; set; } = 10;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public OwUdpClientOptions Value => this;
    }

    /// <summary>
    /// 数据到达事件的事件参数类。
    /// </summary>
    public class UdpDataRecivedEventArgs : EventArgs
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="remotePoint"></param>
        public UdpDataRecivedEventArgs(byte[] data, IPEndPoint remotePoint)
        {
            Data = data;
            RemotePoint = remotePoint;
        }

        /// <summary>
        /// 接受的数据。
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// 接受数据来自的远程终结点。
        /// </summary>
        public IPEndPoint RemotePoint { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class OwUdpHeader
    {
        /// <summary>
        /// 此包包号。
        /// </summary>
        public int Seq;

        /// <summary>
        /// 已经收到的连续包的最大包号。
        /// </summary>
        public int ReceivedMaxSeq;

        /// <summary>
        /// 已经确定丢失的包范围，Item1=最小包号，Item2=最大包号。
        /// </summary>
        public (int, int)[] Lakes;
    }

    /// <summary>
    /// 模拟全双工通讯的Udp工具类。
    /// </summary>
    public class OwUdpClient : IDisposable
    {
        /// <summary>
        /// 默认构造函数。
        /// </summary>
        public OwUdpClient()
        {
            _Options = new OwUdpClientOptions().Value;
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="options"></param>
        public OwUdpClient(IOptions<OwUdpClientOptions> options)
        {
            _Options = options.Value;
            Initialize();
        }

        void Initialize()
        {
            _RequestStop = CancellationTokenSource.CreateLinkedTokenSource(_Options.RequestStop);

            _IoWorker = Task.Factory.StartNew(IoWorker, TaskCreationOptions.LongRunning);
        }

        OwUdpClientOptions _Options;

        UdpClient _UdpClient;
        private Task _IoWorker;

        /// <summary>
        /// 内部的取消标记。
        /// </summary>
        CancellationTokenSource _RequestStop;

        /// <summary>
        /// 收发复位终止的信号。
        /// </summary>
        CancellationTokenSource _Stopped = new CancellationTokenSource();

        ConcurrentQueue<(byte[], IPEndPoint)> _ReceiveQueue = new ConcurrentQueue<(byte[], IPEndPoint)>();

        ConcurrentQueue<(byte[], IPEndPoint)> _SendQueue = new ConcurrentQueue<(byte[], IPEndPoint)>();

        class SendEntry
        {
            public SendEntry()
            {

            }

            public IPEndPoint RemoteEndPoint;

            /// <summary>
            /// 当前可用的序号。
            /// </summary>
            public int Seq = 0;

            /// <summary>
            /// 待发送队列。
            /// </summary>
            public ConcurrentQueue<byte[]> Buffer = new ConcurrentQueue<byte[]>();

            /// <summary>
            /// 已经发送的队列。
            /// </summary>
            public ConcurrentDictionary<int, byte[]> History = new ConcurrentDictionary<int, byte[]>();
        }

        ConcurrentDictionary<IPEndPoint, SendEntry> _SendEntry = new ConcurrentDictionary<IPEndPoint, SendEntry>();

        private bool disposedValue;

        /// <summary>
        /// 获取使用的本地终结点。可能是null。
        /// </summary>
        public IPEndPoint LocalEndPoint => _UdpClient?.Client.LocalEndPoint as IPEndPoint;

        /// <summary>
        /// 数据到达事件。该事件可能在任何线程中引发。
        /// </summary>
        public event EventHandler<UdpDataRecivedEventArgs> UdpDataRecived;

        /// <summary>
        /// 引发数据到达的事件的函数。
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnUdpDataRecived(UdpDataRecivedEventArgs e) => UdpDataRecived?.Invoke(this, e);

        /// <summary>
        /// 对指定端口发送数据。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="remotePoint"></param>
        public void Send(byte[] data, IPEndPoint remotePoint)
        {
            _SendQueue.Enqueue((data, new IPEndPoint(remotePoint.Address, remotePoint.Port)));
        }

        /// <summary>
        /// 用户收发网络数据的工作函数。
        /// </summary>
        void IoWorker()
        {
            #region 初始化

            TimeSpan delay = TimeSpan.FromMilliseconds(1000f / _Options.SendPerSeconds);    //默认延时

            _UdpClient = new UdpClient(_Options.LocalEndPoint);
            _UdpClient.Client.SendBufferSize = _Options.SendBufferSize;
            _UdpClient.Client.ReceiveBufferSize = _Options.ReceiveBufferSize;

            var ct = _RequestStop.Token;
            #endregion 初始化

            while (!_RequestStop.IsCancellationRequested)   //当未要求退出时
            {
                try
                {
                    while (_UdpClient.Available > 0)    //当有数据可以读取时
                    {
                        IPEndPoint listen = new IPEndPoint(_Options.RemoteEndPoint.Address, _Options.RemoteEndPoint.Port);
                        var data = _UdpClient.Receive(ref listen);
                        _ReceiveQueue.Enqueue((data, listen));
                    }
                    if (_ReceiveQueue.Count > 0) Task.Run(RaiseEvent);   //若需要引发事件
                    while (_SendQueue.TryDequeue(out var data))
                    {
                        _UdpClient.Send(data.Item1, data.Item1.Length, data.Item2);
                    }
                }
                catch (ObjectDisposedException)  //已关闭基础 Socket。
                {
                    //目前认为不可能发生
                    throw;
                }
                catch
                {
                    ResetError();
                    Thread.Yield();
                    continue;   //尽快重试
                }
                ct.WaitHandle.WaitOne(delay);   //等待到期再次轮询
            }
            _Stopped.Cancel();  //发出已终止信号
        }

        /// <summary>
        /// 引发事件。
        /// </summary>
        void RaiseEvent()
        {
        lbStart:
            try
            {
                while (_ReceiveQueue.TryDequeue(out var item))
                {
                    OnUdpDataRecived(new UdpDataRecivedEventArgs(item.Item1, item.Item2));
                }
            }
            catch (Exception)
            {
                goto lbStart;
            }
        }

        void ResetError()
        {
            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint IOC_UDP_RESET = IOC_IN | IOC_VENDOR | 12;
            _UdpClient.Client.IOControl((int)IOC_UDP_RESET, new byte[] { Convert.ToByte(false) }, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    _RequestStop.Cancel();
                    _Stopped.Token.WaitHandle.WaitOne(3000);
                    _UdpClient?.Dispose();
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _UdpClient = null;
                _IoWorker = null;
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~OwUdpClient()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}
