namespace Gy001Tools
{
    partial class UdpTools
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btSend = new System.Windows.Forms.Button();
            this.tbSend = new System.Windows.Forms.TextBox();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.btRecv = new System.Windows.Forms.Button();
            this.tbRecv = new System.Windows.Forms.TextBox();
            this.btStop = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btSend
            // 
            this.btSend.Location = new System.Drawing.Point(494, 52);
            this.btSend.Name = "btSend";
            this.btSend.Size = new System.Drawing.Size(75, 23);
            this.btSend.TabIndex = 0;
            this.btSend.Text = "发送(&S)";
            this.btSend.UseVisualStyleBackColor = true;
            this.btSend.Click += new System.EventHandler(this.btSend_Click);
            // 
            // tbSend
            // 
            this.tbSend.Location = new System.Drawing.Point(272, 54);
            this.tbSend.Name = "tbSend";
            this.tbSend.Size = new System.Drawing.Size(216, 21);
            this.tbSend.TabIndex = 1;
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Location = new System.Drawing.Point(-1, -3);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(242, 441);
            this.propertyGrid1.TabIndex = 2;
            // 
            // btRecv
            // 
            this.btRecv.Location = new System.Drawing.Point(494, 115);
            this.btRecv.Name = "btRecv";
            this.btRecv.Size = new System.Drawing.Size(75, 23);
            this.btRecv.TabIndex = 3;
            this.btRecv.Text = "侦听";
            this.btRecv.UseVisualStyleBackColor = true;
            this.btRecv.Click += new System.EventHandler(this.btRecv_Click);
            // 
            // tbRecv
            // 
            this.tbRecv.Location = new System.Drawing.Point(272, 116);
            this.tbRecv.Name = "tbRecv";
            this.tbRecv.Size = new System.Drawing.Size(216, 21);
            this.tbRecv.TabIndex = 4;
            // 
            // btStop
            // 
            this.btStop.Location = new System.Drawing.Point(589, 114);
            this.btStop.Name = "btStop";
            this.btStop.Size = new System.Drawing.Size(75, 23);
            this.btStop.TabIndex = 5;
            this.btStop.Text = "button1";
            this.btStop.UseVisualStyleBackColor = true;
            this.btStop.Click += new System.EventHandler(this.btStop_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(323, 227);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // UdpTools
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btStop);
            this.Controls.Add(this.tbRecv);
            this.Controls.Add(this.btRecv);
            this.Controls.Add(this.propertyGrid1);
            this.Controls.Add(this.tbSend);
            this.Controls.Add(this.btSend);
            this.Name = "UdpTools";
            this.Text = "UdpTools";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UdpTools_FormClosing);
            this.Load += new System.EventHandler(this.UdpTools_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btSend;
        private System.Windows.Forms.TextBox tbSend;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.Button btRecv;
        private System.Windows.Forms.TextBox tbRecv;
        private System.Windows.Forms.Button btStop;
        private System.Windows.Forms.Button button1;
    }
}