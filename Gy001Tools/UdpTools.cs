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
        CancellationTokenSource _CancellationTokenSource;

        private void UdpTools_Load(object sender, EventArgs e)
        {
            _Options = new UdpToolsOptions
            {
                IP = "192.168.0.104",
                Port = 0,
                RecvIp = "192.168.0.104",
                RecvPort = 20090,
            };

            propertyGrid1.SelectedObject = _Options;
        }

        private void btSend_Click(object sender, EventArgs e)
        {
            var bin = Encoding.UTF8.GetBytes(tbSend.Text);
            var rip = new IPEndPoint(IPAddress.Parse(_Options.RecvIp), _Options.RecvPort);
            using (var udp = new UdpClient(_Options.Port))
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
                    CancellationTokenSource cts = new CancellationTokenSource(3000);
                    var udp = new UdpClient(_Options.RecvPort);
                    {
                        var rip = new IPEndPoint(IPAddress.Parse(_Options.RecvIp), _Options.RecvPort);
                        Task.Run(() =>
                        {
                            while (udp.Available <= 0) Thread.Sleep(1);
                            var buff = udp.Receive(ref rip);
                            var str = Encoding.UTF8.GetString(buff, 0, buff.Length);
                            Invoke(new Action(() =>
                            {
                                tbRecv.Text = $"From{rip.Address}:{rip.Port},Data:{str}";
                            }));
                        });
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

        private void UdpTools_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();
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
