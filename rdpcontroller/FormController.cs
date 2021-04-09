using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace rdpcontroller
{
    public partial class FormController : Form
    {
        RdpTcpClient m_rdptcpclient = new RdpTcpClient();

        public string m_code;
        public string m_pwd;

        public FormController(string code,string pwd)
        {
            m_code = code;
            m_pwd = pwd;
            InitializeComponent();
        }

        private void FormController_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;

            if (string.IsNullOrEmpty(m_code))
            {
                Close();
                return;
            }

            if (m_rdptcpclient.Connect(rdpcommon.Protocol.SERVER_HOST, rdpcommon.Protocol.CONTROLLER_PORT, "0|" + m_code + "|" + m_pwd, OnTcpClose, axRDPViewerF, axMsRdpClient7NotSafeForScripting1))
            {
                return;
            }
            if (m_rdptcpclient.m_startres == "PWDERROR")
            {
                MessageBox.Show("密码错误");
                Close();
                return;
            }
            if (m_rdptcpclient.Connect(rdpcommon.Protocol.SERVER_HOST, rdpcommon.Protocol.CONTROLLER_PORT, "0|" + m_code + "|" + m_pwd, OnTcpClose, axRDPViewerF, axMsRdpClient7NotSafeForScripting1))
            {
                return;
            }
            if (string.IsNullOrEmpty(m_rdptcpclient.m_startres))
            {
                MessageBox.Show("连接失败");
            }
            else
            {
                MessageBox.Show("连接失败,error=" + m_rdptcpclient.m_startres);
            }
            Close();
        }

        private void OnTcpClose()
        {
            try
            {
                Close();
            }
            catch (Exception e)
            {
            }
        }

        private void FormController_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_rdptcpclient.DisConnect();
        }
    }
}
