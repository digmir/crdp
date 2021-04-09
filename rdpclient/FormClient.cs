using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace rdpclient
{
    public partial class FormClient : Form
    {
        RdpTcpClient rdptcpclient = new RdpTcpClient();

        string pwdcode = null;
        int connectstatus = -1;

        Thread thread;

        public FormClient()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        static void BackThread(object param)
        {
            FormClient pThis = (FormClient)param;
            while (!pThis.IsDisposed)
            {
                if (pThis.connectstatus == 0)
                {
                    pThis.OnTcpClose();
                }
                Thread.Sleep(500);
            }
        }

        private void FormClient_Load(object sender, EventArgs e)
        {
            RegistryKey hk = null;
            try
            {
                //HKEY_CURRENT_USER/System
                hk = Registry.CurrentUser
                    .OpenSubKey("System",true);
                pwdcode = (string)hk.GetValue("RDP-PWDCODE");
            }
            catch (Exception)
            {
            }
            if (string.IsNullOrEmpty(pwdcode))
            {
                Random rd = new Random();
                char[] CODELIST = "abcdefghijkmnpqrstuvwxyz0123456789".ToCharArray();
                for (int i = 0; i < 4; i++)
                {
                    int n = rd.Next(0, CODELIST.Length);
                    pwdcode += CODELIST[n % CODELIST.Length];
                }

                if (hk != null)
                {
                    try
                    {
                        hk.SetValue("RDP-PWDCODE", pwdcode, RegistryValueKind.String);
                    }
                    catch
                    {
                    }
                }
            }
            textBoxPassword.Text = pwdcode;
            if (hk != null)
            {
                try
                {
                    hk.Close();
                }
                catch
                {
                }
            }

            btnChgPwd.Hide();

            thread = new Thread(new ParameterizedThreadStart(BackThread));
            thread.Start(this);

            connectstatus = 0;
        }

        private void OnTcpClose()
        {
            if (connectstatus == -1) {
                return;
            }
            connectstatus = -1;
            try
            {
                textBoxCode.Text = "加载中...";
                if (rdptcpclient.Connect(rdpcommon.Protocol.SERVER_HOST, rdpcommon.Protocol.CLIENT_PORT, OnTcpClose, pwdcode))
                {
                    textBoxCode.Text = rdptcpclient.StrCode;
                    connectstatus = 1;
                }
                else
                {
                    textBoxCode.Text = "加载失败";
                    connectstatus = 0;
                }
            }
            catch
            {
                connectstatus = 0;
            }
        }

        private void btnChgPwd_Click(object sender, EventArgs e)
        {
            textBoxPassword.Text = textBoxPassword.Text.Trim();
            if(string.IsNullOrEmpty(textBoxPassword.Text))
            {
                MessageBox.Show("密码不能为空");
                textBoxPassword.Text = pwdcode;
                return;
            }
            RegistryKey hk = null;
            try
            {
                //HKEY_CURRENT_USER/System
                hk = Registry.CurrentUser
                    .OpenSubKey("System", true);
                hk.SetValue("RDP-PWDCODE", textBoxPassword.Text,RegistryValueKind.String);
                hk.Close();
            }
            catch (Exception)
            {
            }
            pwdcode = textBoxPassword.Text;
            btnChgPwd.Hide();
            rdptcpclient.Disconnect();
        }

        private void textBoxPassword_TextChanged(object sender, EventArgs e)
        {
            btnChgPwd.Show();
        }
    }
}
