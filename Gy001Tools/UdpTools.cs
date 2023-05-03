using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gy001Tools
{
    public partial class UdpTools : Form
    {
        public UdpTools()
        {
            InitializeComponent();
        }

        UdpToolsOptions _Options = new UdpToolsOptions { };
        Thread _RecvTask;
        CancellationTokenSource _CancellationTokenSource;

        private void UdpTools_Load(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = _Options;
        }

        private void btSend_Click(object sender, EventArgs e)
        {
            var bin = Encoding.UTF8.GetBytes(tbSend.Text);
            var rip = new IPEndPoint(IPAddress.Parse(_Options.IP), _Options.Port);
            using (var udp = new UdpClient(_Options.RecvPort))
            {
                udp.Send(bin, bin.Length, rip);
            }
        }

        private void btRecv_Click(object sender, EventArgs e)
        {
            //_CancellationTokenSource?.Cancel();
            //_RecvTask?.Join();

            _CancellationTokenSource = new CancellationTokenSource();
            //_RecvTask = new Thread(() =>
            {
                try
                {
                    using (var udp = new UdpClient(_Options.RecvPort))
                    {
                        var rip = new IPEndPoint(IPAddress.Parse(_Options.RecvIp), _Options.RecvPort);
                        var data = udp.Receive(ref rip);
                        var str = Encoding.UTF8.GetString(data, 0, data.Length);
                        tbRecv.Text = $"From{rip.Address}:{rip.Port},Data:{str}";
                        udp.Send(data, data.Length, rip);
                    }
                }
                catch (ThreadAbortException)
                {

                }
            };
            //_RecvTask.Start();
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            _CancellationTokenSource?.Cancel();
            _RecvTask?.Abort();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var udp = new UdpClient(_Options.RecvPort, AddressFamily.InterNetwork);
            {
                Task.Run(() =>
                {
                    {
                        var rip = new IPEndPoint(IPAddress.Parse(_Options.RecvIp), ((IPEndPoint)udp.Client.LocalEndPoint).Port);
                        var data = udp.Receive(ref rip);
                        var str = Encoding.UTF8.GetString(data, 0, data.Length);
                        tbRecv.Text = $"From{rip.Address}:{rip.Port},Data:{str}";
                    }
                });
                var bin = Encoding.UTF8.GetBytes(tbSend.Text);
                var rip1 = new IPEndPoint(IPAddress.Parse(_Options.IP), _Options.Port);
                Thread.Sleep(1000);
                udp.Send(bin, bin.Length, rip1);
            }
        }
    }

    public class UdpToolsOptions
    {
        public string IP { get; set; } = "43.133.232.4";

        public ushort Port { get; set; }

        public string RecvIp { get; set; } = "0.0.0.0";

        public ushort RecvPort { get; set; }
    }
}
