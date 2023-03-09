using Gy02.Publisher;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gy02Publisher
{

    /// <summary>
    /// 帮助器类。
    /// </summary>
    public static class ServerHelper
    {
    }

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

        UdpClient _Udp;
        Task _Task;
        /// <summary>
        /// 开始侦听。在登录完成后调用此函数，开始侦听数据。
        /// </summary>
        public void Start()
        {
            var ary = LastUdpServiceHost.Split(':');
            var ip = new IPEndPoint(IPAddress.Parse(ary[0]), int.Parse(ary[1]));
            Start(LastToken, ip);
        }

        /// <summary>
        /// 开始侦听。
        /// </summary>
        public void Start(Guid token, IPEndPoint remotePoint)
        {
            _Udp?.Dispose();
            _Udp = new UdpClient(0);

            var guts = token.ToByteArray();
            _Udp.Send(guts, guts.Length, remotePoint);

            _Task = Task.Factory.StartNew(c =>
            {
                UdpClient udp = (UdpClient)c;
                var ip = new IPEndPoint(remotePoint.Address, 0);
                while (true)
                {
                    var buff = udp.Receive(ref ip);
                    try
                    {
                        OnDataRecived(new DataRecivedEventArgs()
                        {
                            Data = buff,
                        });
                    }
                    catch (Exception excp)
                    {
                        Debug.WriteLine(excp);
                    }
                }
            }, _Udp, TaskCreationOptions.LongRunning);
        }

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
        /// <inheritdoc/>
        /// </summary>
        public void Dispose()
        {
            _Udp?.Dispose();
            _Udp = null;
            GC.SuppressFinalize(this);
        }
    }
}
