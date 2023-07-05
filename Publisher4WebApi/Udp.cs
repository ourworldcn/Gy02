using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using GY02.Templates;
using System.Timers;

namespace GY02.Publisher
{
    /// <summary>
    /// 示例。
    /// </summary>
    public static class DispatcherDemo
    {
        static Dictionary<Type, MethodInfo> _Dic = new Dictionary<Type, MethodInfo>();

        static IReadOnlyDictionary<Type, MethodInfo> Dic
        {
            get
            {
                if (_Dic is null) { }
                return _Dic;
            }
        }
        static void Gen()
        {
            MethodInfo mi = default;
            object obj = default;
            mi.Invoke(null, new object[] { obj });
        }

        /// <summary>
        /// 示例。
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int M1(ListenStartedDto data) { return 0; }

        static ConcurrentDictionary<Guid, object> _dic = new ConcurrentDictionary<Guid, object>();

        static void GenUi()
        {
            foreach (var item in _dic)
            {
            }
        }
    }

    /// <summary>
    /// <see cref="GyUdpClient.DataRecived"/>事件的数据类。
    /// </summary>
    public class DataRecivedEventArgs : EventArgs
    {
        #region 静态成员

        static Dictionary<Guid, Type> _Types;

        /// <summary>
        /// 所有Json可转换的类型
        /// </summary>
        public static IReadOnlyDictionary<Guid, Type> Types
        {
            get
            {
                if (_Types is null)
                {
                    var tmp = AppDomain.CurrentDomain.GetAssemblies().SelectMany(c => c.GetTypes()).Where(c => c.IsClass && typeof(IJsonData).IsAssignableFrom(c)).ToDictionary(c => c.GUID, c => c);
                    Interlocked.CompareExchange(ref _Types, tmp, null); //可能过度初始化
                }
                return _Types;
            }
        }
        #endregion 静态成员

        /// <summary>
        /// 到达的数据。
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// 获取Data中数据的Type的唯一Id和Json字符串。
        /// </summary>
        /// <param name="json">Json字符串/</param>
        /// <returns>数据的类型。未能找到合适类型，则可能返回null。</returns>
        public Type GetDataType(out string json)
        {
            try
            {
                json = Encoding.UTF8.GetString(Data.Skip(16).ToArray());
                var guid = new Guid(Data.Take(16).ToArray());
                return Types.TryGetValue(guid, out var result) ? result : null;
            }
            catch (Exception excp)
            {
                Debug.WriteLine(excp);
                json = null;
                return null;
            }
        }
    }

    /// <summary>
    /// Udp连接的帮助类。
    /// 在登录用户后调用<see cref="Start()"/>开始侦听。
    /// </summary>
    public class GyUdpClient : IDisposable
    {
        #region 静态成员

        /// <summary>
        /// 暂存最后一次连接服务器的地址。
        /// </summary>
        public static string LastUdpServiceHost { get; set; }

        /// <summary>
        /// 暂存最后一次登录的Token。
        /// </summary>
        public static Guid LastToken { get; set; }

        #endregion  静态成员

        /// <summary>
        /// 默认构造函数。
        /// </summary>
        public GyUdpClient()
        {
        }

        volatile UdpClient _Udp;

        /// <summary>
        /// 引发事件的任务。
        /// </summary>
        Task _PostEventTask;
        /// <summary>
        /// 侦听任务。
        /// </summary>
        Task _ListenTask;

        CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// 请求停止的终止标志。
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get => _CancellationTokenSource; }

        IPEndPoint _RemoteEndPoint;
        /// <summary>
        /// 远程服务器的地址。
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (_RemoteEndPoint is null)
                {
                    var ary = LastUdpServiceHost.Split(':');
                    _RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ary[0]), int.Parse(ary[1]));
                }
                return _RemoteEndPoint;
            }
            set
            {
                _RemoteEndPoint = value;
            }
        }

        Guid? _Token;
        /// <summary>
        /// 最后使用的令牌。
        /// </summary>
        public Guid Token { get => _Token ?? (_Token = LastToken).Value; }

        System.Threading.Timer _Timer;

        /// <summary>
        /// 开始侦听。在登录完成后调用此函数，开始侦听数据。
        /// </summary>
        public void Start()
        {
            _RemoteEndPoint = null;
            Start(LastToken, RemoteEndPoint);
        }
        /// <summary>
        /// 开始侦听。
        /// </summary>
        public virtual void Start(Guid token, IPEndPoint remotePoint)
        {
            _RemoteEndPoint = remotePoint;
            _Udp?.Dispose();
            _Udp = new UdpClient(0);
            //初始化引发事件数据的任务
            if (_PostEventTask is null)
                _PostEventTask = Task.Factory.StartNew(PostEventCallback, TaskCreationOptions.LongRunning);

            //初始化接受网络数据的任务。
            if (_ListenTask is null)
                _ListenTask = Task.Factory.StartNew(ListenCallback, TaskCreationOptions.LongRunning);
            //通知服务器
            //_Timer = new System.Threading.Timer(c => Nop(Token), default, 0, 60_000);
        }

        void ListenCallback()
        {
            Nop(Token);
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var ip = new IPEndPoint(RemoteEndPoint.Address, 0);
                    var buff = _Udp.Receive(ref ip);
                    Debug.WriteLine($"收到来自{ip}的数据，{buff.Length}字节。");
                    InvokeDataRecived(new DataRecivedEventArgs()
                    {
                        Data = buff,
                    });
                }
                catch (Exception excp)
                {
                    Debug.WriteLine(excp);
                }
            }
        }

        /// <summary>
        /// 通知服务器客户端在线。
        /// </summary>
        /// <param name="token"></param>
        public void Nop(Guid token)
        {
            //通知服务器
            var guts = token.ToByteArray();
            _Udp.Send(guts, guts.Length, RemoteEndPoint);
        }

        #region 事件相关

        /// <summary>
        /// 有数据到达的事件。
        /// 此事件发生在后台线程中。
        /// </summary>
        public event EventHandler<DataRecivedEventArgs> DataRecived;

        /// <summary>
        /// 引发<see cref="DataRecived"/>事件。
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnDataRecived(DataRecivedEventArgs e) => DataRecived?.Invoke(this, e);

        /// <summary>
        /// 向排队增加一个事件数据。
        /// </summary>
        /// <param name="e"></param>
        public void InvokeDataRecived(DataRecivedEventArgs e) => _EventDatas.Add(e);

        BlockingCollection<DataRecivedEventArgs> _EventDatas = new BlockingCollection<DataRecivedEventArgs>();

        /// <summary>
        /// 将事件数据异步引发的线程函数。
        /// </summary>
        void PostEventCallback()
        {
            DataRecivedEventArgs item;
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    _EventDatas.TryTake(out item);

                }
                catch (ObjectDisposedException) //已释放了 BlockingCollection<T>。
                {
                    return;
                }
                catch (InvalidOperationException)   //该基础集合已在此 BlockingCollection<T> 实例外部进行了修改。
                {
                    //不可能出现
                    throw;
                }
                try
                {
                    OnDataRecived(item);
                }
                catch (Exception)
                {
                }
            }
        }
        #endregion 事件相关

        #region IDisposable接口相关

        /// <summary>
        /// 如果对象已经被处置则抛出<see cref="ObjectDisposedException"/>异常。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //[DoesNotReturn]
        protected void ThrowIfDisposed()
        {
            if (_IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        private bool _IsDisposed;

        /// <summary>
        /// 获取或设置对象是否已经处置的属性，派生类需要自己切换该属性。
        /// </summary>
        protected bool IsDisposed { get => _IsDisposed; set => _IsDisposed = value; }

        /// <summary>
        /// 调用此实现以切换 <see cref="IsDisposed"/> 属性。
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    //释放托管状态(托管对象)
                    _CancellationTokenSource?.Cancel();
                    var waitHandle = new AutoResetEvent(false);
                    _Timer?.Dispose(/*waitHandle*/);
                    //waitHandle.WaitOne();   //确保定时器退出
                    _Udp?.Dispose();
                }
                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Udp = null;
                _PostEventTask = null;
                //base.Dispose(disposing);  //        IsDisposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~LeafMemoryCache()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// 处置对象。
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口相关
    }

}