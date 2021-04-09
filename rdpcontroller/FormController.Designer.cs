namespace rdpcontroller
{
    partial class FormController
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormController));
            this.axRDPViewerF = new AxRDPCOMAPILib.AxRDPViewer();
            this.axMsRdpClient7NotSafeForScripting1 = new AxMSTSCLib.AxMsRdpClient7NotSafeForScripting();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.axRDPViewerF)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axMsRdpClient7NotSafeForScripting1)).BeginInit();
            this.SuspendLayout();
            // 
            // axRDPViewerF
            // 
            this.axRDPViewerF.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axRDPViewerF.Enabled = true;
            this.axRDPViewerF.Location = new System.Drawing.Point(0, 0);
            this.axRDPViewerF.Name = "axRDPViewerF";
            this.axRDPViewerF.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axRDPViewerF.OcxState")));
            this.axRDPViewerF.Size = new System.Drawing.Size(669, 454);
            this.axRDPViewerF.TabIndex = 2;
            this.axRDPViewerF.Visible = false;
            // 
            // axMsRdpClient7NotSafeForScripting1
            // 
            this.axMsRdpClient7NotSafeForScripting1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axMsRdpClient7NotSafeForScripting1.Enabled = true;
            this.axMsRdpClient7NotSafeForScripting1.Location = new System.Drawing.Point(0, 0);
            this.axMsRdpClient7NotSafeForScripting1.Name = "axMsRdpClient7NotSafeForScripting1";
            this.axMsRdpClient7NotSafeForScripting1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axMsRdpClient7NotSafeForScripting1.OcxState")));
            this.axMsRdpClient7NotSafeForScripting1.Size = new System.Drawing.Size(669, 454);
            this.axMsRdpClient7NotSafeForScripting1.TabIndex = 4;
            this.axMsRdpClient7NotSafeForScripting1.Visible = false;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(290, 78);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 12);
            this.label1.TabIndex = 5;
            this.label1.Text = "连接中。。。";
            // 
            // FormController
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(669, 454);
            this.Controls.Add(this.axRDPViewerF);
            this.Controls.Add(this.axMsRdpClient7NotSafeForScripting1);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormController";
            this.Text = "远程控制端";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormController_FormClosed);
            this.Load += new System.EventHandler(this.FormController_Load);
            ((System.ComponentModel.ISupportInitialize)(this.axRDPViewerF)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axMsRdpClient7NotSafeForScripting1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private AxRDPCOMAPILib.AxRDPViewer axRDPViewerF;
        private AxMSTSCLib.AxMsRdpClient7NotSafeForScripting axMsRdpClient7NotSafeForScripting1;
        private System.Windows.Forms.Label label1;
    }
}

