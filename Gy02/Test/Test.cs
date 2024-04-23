using GY02.Managers;
using GY02.Publisher;
using Microsoft.Extensions.Options;
using System.Net;

namespace Gy02.Test
{
    /// <summary>
    /// 测试udp的用例。
    /// </summary>
    public class TestUdpServerManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        public TestUdpServerManager(IServiceProvider serviceProvider)
        {

            _ServiceProvider = serviceProvider;
            _Server = _ServiceProvider.GetRequiredService<UdpServerManager>();
            _Client = new GyUdpClient();
            var option = _ServiceProvider.GetRequiredService<IOptions<UdpServerManagerOptions>>();
            _Client.DataRecived += _Client_DataRecived;
        }

        private void _Client_DataRecived(object? sender, DataRecivedEventArgs e)
        {
        }

        IServiceProvider _ServiceProvider;
        UdpServerManager _Server;
        GyUdpClient _Client;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="endPoing"></param>
        public void Test(Guid token, IPEndPoint endPoing)
        {
            _Client.Start(token, endPoing);
        }
    }
}
