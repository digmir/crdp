using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Management;
using rdpcommon;
using RDPCOMAPILib;
using System.Xml;
using Microsoft.Win32;

namespace rdpclient
{
    public delegate void AsyncCloseCallback();

    class RdpTcpClient
    {
        const int RECV_BUFF_SIZE = 1024 * 1024;

        RDPSession m_pRdpSession;
        TcpClient m_tcpclient = null;
        Protocol m_protocol = new Protocol(null);
        byte[] m_buffer;
        AsyncCloseCallback m_closecallack;
        TcpClient m_tcprdp = null;
        string m_rdphost;
        int m_rdpport = 0;
        byte[] m_rdpbuffer;
        string m_rdpConnectionString;
        int m_rdpviewtype = 0; //0:RDPView, 1:RDPClient

        int m_start = 0;

        QOS m_qos = new QOS();

        string m_pwdcode = "";

        public string StrCode { get; private set; }


        public RdpTcpClient()
        {
            try
            {
                m_pRdpSession = new RDPSession();
                m_pRdpSession.colordepth = 8;
                m_pRdpSession.SetDesktopSharedRect(0, 0, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
                m_pRdpSession.OnAttendeeConnected += new _IRDPSessionEvents_OnAttendeeConnectedEventHandler(OnAttendeeConnected);
                m_pRdpSession.OnAttendeeDisconnected += new _IRDPSessionEvents_OnAttendeeDisconnectedEventHandler(OnAttendeeDisconnected);
                m_pRdpSession.OnControlLevelChangeRequest += new _IRDPSessionEvents_OnControlLevelChangeRequestEventHandler(OnControlLevelChangeRequest);
                m_pRdpSession.Open();
                IRDPSRAPIInvitation invitation = m_pRdpSession.Invitations.CreateInvitation("baseAuth", "groupName", "", 2);
                m_rdpConnectionString = invitation.ConnectionString;
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(invitation.ConnectionString);
                XmlNodeList nlist = doc.SelectNodes("//L");
                if (nlist != null && nlist.Count > 0)
                {
                    for (int i = 0; i < nlist.Count; i++)
                    {
                        XmlAttribute attrhost = nlist[i].Attributes["N"];
                        if (attrhost != null)
                        {
                            if (attrhost.Value.IndexOf(":") > 0)
                            {
                                //ipv6
                            }
                            else if (attrhost.Value.IndexOf(".") > 0)
                            {
                                //ipv4
                                XmlAttribute attrport = nlist[i].Attributes["P"];
                                if (attrport != null)
                                {
                                    int.TryParse(attrport.Value,out m_rdpport);
                                    m_rdphost = attrhost.Value;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Trace(e);
            }
            //<E><A KH="jebWL5nMyWOiBHyxcM08QPxhQ4E=" ID="baseAuth"/><C><T ID="1" SID="3344616568"><L P="5247" N="fe80::dc3d:7c76:b517:75fb%11"/><L P="5248" N="192.168.10.175"/></T></C></E>
        }
        ~RdpTcpClient()
        {
            try
            {
                m_pRdpSession.Close();
            }
            catch (Exception e)
            {

            }
        }

        private void OnAttendeeConnected(object pObjAttendee)
        {
            IRDPSRAPIAttendee pAttendee = pObjAttendee as IRDPSRAPIAttendee;
            pAttendee.ControlLevel = CTRL_LEVEL.CTRL_LEVEL_INTERACTIVE;
        }
        private void OnAttendeeDisconnected(object pDisconnectInfo)
        {
            IRDPSRAPIAttendeeDisconnectInfo pDiscInfo = pDisconnectInfo as IRDPSRAPIAttendeeDisconnectInfo;
        }
        private void OnControlLevelChangeRequest(object pObjAttendee, CTRL_LEVEL RequestedLevel)
        {
            IRDPSRAPIAttendee pAttendee = pObjAttendee as IRDPSRAPIAttendee;
            pAttendee.ControlLevel = RequestedLevel;
        }

        private bool ConnectRdp(int m_rdpport)
        {
            if (m_tcprdp != null)
            {
                m_tcprdp.Close();
                m_tcprdp = null;
            }
            m_tcprdp = new TcpClient();
            try
            {
                if (m_rdpviewtype == 1)
                {
                    //HKEY_LOCAL_MACHINE/SYSTEM/CurrentControlSet/Control/TerminalServer/WinStations/RDP-tcp
                    RegistryKey hk = Registry.LocalMachine
                        .OpenSubKey("SYSTEM")
                        .OpenSubKey("CurrentControlSet")
                        .OpenSubKey("Control")
                        .OpenSubKey("Terminal Server")
                        .OpenSubKey("WinStations")
                        .OpenSubKey("RDP-tcp")
                        ;
                    int PortNumber = (int)hk.GetValue("PortNumber");
                    hk.Close();
                    m_tcprdp.Connect("127.0.0.1", 3389);
                }
                else
                {
                    m_tcprdp.Connect(m_rdphost, m_rdpport);
                }
                try
                {
                    m_rdpbuffer = null;
                    m_rdpbuffer = new byte[RECV_BUFF_SIZE];
                    m_tcprdp.GetStream().BeginRead(m_rdpbuffer, 0, m_rdpbuffer.Length, OnRecvRdpData, this);
                }
                catch (Exception e)
                {
                    Logger.Trace(e);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Trace(e);
                m_tcprdp = null;
            }
            return false;
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <param name="m_closecallack"></param>
        /// <returns></returns>
        public bool Connect(string hostname, int port, AsyncCloseCallback m_closecallack, string pwdcode)
        {
            if (m_rdpport == 0)
            {
                return false;
            }

            if (m_tcpclient != null)
            {
                m_tcpclient.Close();
                m_tcpclient = null;
            }
            
            this.m_closecallack = m_closecallack;

            m_tcpclient = new TcpClient();
            m_tcpclient.ReceiveTimeout = 10000;
            m_tcpclient.SendTimeout = 10000;
            try
            {
                m_tcpclient.Connect(hostname, port);
                if (!Handshake(pwdcode))
                {
                    OnClose(null);
                    return false;
                }
                m_pwdcode = pwdcode;
                m_start = 0;
                if (m_tcprdp != null)
                {
                    try
                    {
                        m_tcprdp.Close();
                    }
                    catch
                    {
                    }
                    m_tcprdp = null;
                }
                m_qos.SetQOS(128 * 1024, 256 * 1024);

                try
                {
                    m_buffer = null;
                    m_buffer = new byte[RECV_BUFF_SIZE];
                    m_tcpclient.GetStream().BeginRead(m_buffer, 0, m_buffer.Length, OnRecvData, this);
                }
                catch (Exception e)
                {
                    Logger.Trace(e);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Trace(e);
                m_tcpclient = null;
            }

            return false;
        }
        public void Disconnect()
        {
            OnClose(null);
        }

        /// <summary>
        /// 接收RDP数据事件
        /// </summary>
        /// <param name="ar"></param>
        private void OnRecvRdpData(IAsyncResult ar)
        {
            int recv = 0;
            try
            {
                recv = m_tcprdp.GetStream().EndRead(ar);
            }
            catch(Exception e)
            {
                Logger.Trace(e);
                recv = 0;
            }

            if (recv <= 0)
            {
                // connection has been closed  
                Logger.Trace("OnClose");
                OnClose(ar);
                return;
            }

            byte[] buff = new byte[recv];
            Array.Copy(m_rdpbuffer, 0, buff, 0, recv);

            WriteData(buff);
            m_qos.DataOut(recv);

            try
            {
                m_tcprdp.GetStream().BeginRead(m_rdpbuffer, 0, m_rdpbuffer.Length, OnRecvRdpData, this);
            }
            catch (Exception e)
            {
                Logger.Trace(e);
                OnClose(ar);
            }
        }

        /// <summary>
        /// 接收数据事件
        /// </summary>
        /// <param name="ar"></param>
        private void OnRecvData(IAsyncResult ar)
        {
            int recv = 0;
            try
            {
                recv = m_tcpclient.GetStream().EndRead(ar);
            }
            catch(Exception e)
            {
                Logger.Trace(e);
                recv = 0;
            }

            if (recv <= 0)
            {
                // connection has been closed  
                Logger.Trace("OnClose");
                OnClose(ar);
                return;
            }

            if(m_start == 0)
            {
                int cmd = 0;
                byte[] data = m_protocol.ParseCmd(m_buffer, recv, ref cmd);
                if (cmd != (int)Protocol.CMD.C2C_RDPSTART || data == null || data.Length == 0)
                {
                    Logger.Trace("OnClose");
                    OnClose(ar);
                    return;
                }
                string sdata = System.Text.UTF8Encoding.UTF8.GetString(data);
                if (string.IsNullOrEmpty(sdata))
                {
                    Logger.Trace("OnClose");
                    OnClose(ar);
                    return;
                }
                string[] rdata = sdata.Split(new char[] { '|' }, 3);
                if (rdata == null || rdata.Length != 3)
                {
                    Logger.Trace("OnClose");
                    OnClose(ar);
                    return;
                }
                int.TryParse(rdata[0], out m_rdpviewtype);
                if (m_pwdcode != rdata[2])
                {
                    Logger.Trace("OnClose");
                    data = m_protocol.C2C_RdpStartRes("PWDERROR");
                    WriteData(data);
                    OnClose(ar);
                    return;
                }

                if (!ConnectRdp(m_rdpport))
                {
                    Logger.Trace("OnClose");
                    data = m_protocol.C2C_RdpStartRes("RDPERROR");
                    WriteData(data);
                    OnClose(ar);
                    return;
                }
                m_start = 1;
                data = m_protocol.C2C_RdpStartRes("OK");
                WriteData(data);
            }
            else
            {
                try
                {
                    NetworkStream stream = m_tcprdp.GetStream();
                    if (stream == null)
                    {
                        Logger.Trace("OnClose");
                        OnClose(null);
                        return;
                    }
                    stream.Write(m_buffer, 0, recv);

                    m_qos.DataIn(recv);
                }
                catch (Exception e)
                {
                    Logger.Trace(e);
                }
            }

            // continue listening for tcp datagram packets 
            try
            {
                m_tcpclient.GetStream().BeginRead(m_buffer, 0, m_buffer.Length, OnRecvData, this);
            }
            catch (Exception e)
            {
                Logger.Trace(e);
                OnClose(ar);
            }
        }

        /// <summary>
        /// 关闭事件
        /// </summary>
        /// <param name="ar"></param>
        private void OnClose(IAsyncResult ar)
        {
            m_start = 0;
            try
            {
                if (m_tcpclient != null)
                {
                    m_tcpclient.Close();
                    m_tcpclient = null;
                }
            }catch(Exception e)
            {

            }
            try
            {
                if (m_tcprdp != null)
                {
                    m_tcprdp.Close();
                    m_tcprdp = null;
                }
            }
            catch (Exception e)
            {

            }
            if (m_closecallack != null)
            {
                m_closecallack();
                m_closecallack = null;
            }
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <returns></returns>
        public byte[] ReadData()
        {
            NetworkStream stream = m_tcpclient.GetStream();
            if (stream == null)
            {
                Logger.Trace("OnClose");
                OnClose(null);
                return null;
            }
            byte[] bytelen = new byte[4];
            try
            {
                if(stream.Read(bytelen, 0, 4) != 4)
                {
                    Logger.Trace("OnClose");
                    OnClose(null);
                    return null;
                }
                int len = BitConverter.ToInt32(bytelen, 0);
                if (len < 4 || len > 1024*1024*8)
                {
                    Logger.Trace("OnClose");
                    OnClose(null);
                    return null;
                }
                byte[] data = new byte[len + 4];
                Array.Copy(bytelen, data, 4);
                int recved = 0;
                while(recved < len)
                {
                    int r = stream.Read(data, recved+4, len - recved);
                    if (r <= 0)
                    {
                        Logger.Trace("OnClose");
                        OnClose(null);
                        return null;
                    }
                    recved += r;
                }

                return data;
            }
            catch (Exception e)
            {
                Logger.Trace(e);
            }
            return null;
        }

        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="data"></param>
        public void WriteData(byte[] data)
        {
            try
            {
                NetworkStream stream = m_tcpclient.GetStream();
                if (stream == null)
                {
                    Logger.Trace("OnClose");
                    OnClose(null);
                    return;
                }
                stream.Write(data, 0, data.Length);
            }catch(Exception e)
            {
                Logger.Trace(e);
            }
        }

        /// <summary>
        /// 跟服务端握手
        /// 发送特征码，获得连接号
        /// </summary>
        /// <returns></returns>
        public bool Handshake(string pwdcode)
        {
            string hostname = Dns.GetHostName();
            string iplist = "";
            IPAddress[] addressList = Dns.GetHostEntry(hostname).AddressList;
            for(int i = 0; i <addressList.Length; i++)
            {
                iplist += addressList[i].ToString() + ";";
            }
            string maclist = "";
            ManagementClass mc;
            mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if (mo["IPEnabled"].ToString() == "True")
                {
                    maclist += mo["MacAddress"].ToString() + ";";
                }
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(m_rdpConnectionString);

            XmlElement newele;
            XmlElement paramele = doc.CreateElement("param");
            newele = doc.CreateElement("name");
            newele.InnerText = hostname;
            paramele.AppendChild(newele);
            newele = doc.CreateElement("mac");
            newele.InnerText = maclist;
            paramele.AppendChild(newele);
            newele = doc.CreateElement("ip");
            newele.InnerText = iplist;
            paramele.AppendChild(newele);
            newele = doc.CreateElement("pwd");
            newele.InnerText = pwdcode;
            paramele.AppendChild(newele);

            try
            {
                //HKEY_CURRENT_USER/System
                RegistryKey hk = Registry.CurrentUser
                    .OpenSubKey("System");
                string regcode = (string)hk.GetValue("RDP-STRCODE");
                if(!string.IsNullOrEmpty(regcode))
                {
                    newele = doc.CreateElement("code");
                    newele.InnerText = regcode;
                    paramele.AppendChild(newele);
                }
                hk.Close();
            }
            catch (Exception e)
            {

            }

            doc.FirstChild.AppendChild(paramele);

            newele = doc.CreateElement("rdptype");
            newele.InnerText = m_rdpviewtype.ToString();
            doc.FirstChild.AppendChild(newele);

            byte[] data = m_protocol.C2S_Handshake(doc.InnerXml);
            WriteData(data);

            int cmd = 0;
            data = ReadData();
            data = m_protocol.ParseCmd(data, ref cmd);
            if (cmd != (int)Protocol.CMD.S2C_HANDSHAKE)
            {
                return false;
            }
            StrCode = System.Text.UTF8Encoding.UTF8.GetString(data);

            try
            {
                //HKEY_CURRENT_USER/System
                RegistryKey hk = Registry.CurrentUser
                    .OpenSubKey("System",true);
                hk.SetValue("RDP-STRCODE", StrCode,RegistryValueKind.String);
                hk.Close();
            }
            catch (Exception e)
            {
                Logger.Trace(e);
            }
            return true;
        }
    }
}
