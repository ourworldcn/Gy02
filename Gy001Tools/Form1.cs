using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gy001Tools
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btGen_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(tbCount.Text, out int count))
            {
                MessageBox.Show("请输入生成的数量。");
                tbCount.Focus();
                tbCount.SelectAll();
            }
            List<Guid> lst = new List<Guid>();
            for (int i = 0; i < count; i++)
            {
                lst.Add(Guid.NewGuid());
            }
            var guts = string.Join(Environment.NewLine, lst.Select(c => c.ToString("B")));

            tbGuts.Text = guts;

            tbClientGuts.Text = string.Join(Environment.NewLine, lst.Select(c => Convert.ToBase64String(c.ToByteArray())));
            tbGuts.Focus();
            tbGuts.SelectAll();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            sw.Stop();

            try
            {
                TestExcel();
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);
            }
        }

        private void TestExcel()
        {
            try
            {
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);
            }
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
        bool _Changing;

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (_Changing) return;
            _Changing = true;
            if (Guid.TryParse(textBox1.Text, out var guid)) textBox2.Text = Convert.ToBase64String(guid.ToByteArray());
            else
                textBox2.Text = "";
            _Changing = false;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (_Changing) return;
            _Changing = true;
            try
            {
                var bin = Convert.FromBase64String(textBox2.Text);
                var guid = new Guid(bin);
                textBox1.Text = guid.ToString();
            }
            catch (Exception)
            {
                textBox1.Text = "";
            }
            finally { _Changing = false; }
        }
    }
}
