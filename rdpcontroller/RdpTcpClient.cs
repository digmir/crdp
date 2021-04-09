using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using rdpcommon;
using RDPCOMAPILib;
using System.Xml;

namespace rdpcontroller
{
    public delegate void AsyncCloseCallback();
    class RdpTcpClient
    {
        const int RECV_BUFF_SIZE = 1024 * 1024;

        TcpClient m_tcpclient = null;
        Protocol m_protocol = new Protocol(null);
        byte[] m_buffer;
        AsyncCloseCallback m_closecallack;

        string m_rdpConnectionString;

        TcpListener m_RdpListener;
        TcpClient m_rdpclient;
        byte[] m_rdpbuffer;

        int m_rdpviewtype;
        AxRDPCOMAPILib.AxRDPViewer m_viewer;
        AxMSTSCLib.AxMsRdpClient7NotSafeForScripting m_viewer2;

        QOS m_qos = new QOS();
        public string m_startres = "";

        private bool OpenRDPView()
        {
            if (m_RdpListener != null)
            {
                try
                {
                    m_RdpListener.Stop();
                    m_RdpListener = null;
                }
                catch(Exception e)
                {
                }
            }

            int rdpport = 20000;
            for (int i = 0; i < 10; i++,rdpport++)
            {
                try
                {
                    m_RdpListener = new TcpListener(IPAddress.Any, rdpport);
                    break;
                }
                catch (Exception e)
                {
                    m_RdpListener = null;
                }
            }
            if (m_RdpListener == null)
            {
                return false;
            }
            try
            {
                m_RdpListener.Start();
                m_RdpListener.BeginAcceptTcpClient(OnRdpConnect, this);
            }
            catch (Exception e)
            {
                m_RdpListener = null;
                return false;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(m_rdpConnectionString);
            XmlNode node = doc.SelectSingleNode("//E/C/T");
            if (node == null)
            {
                m_RdpListener.Stop();
                m_RdpListener = null;
                return false;
            }
            node.InnerXml = "";
            XmlElement newele = doc.CreateElement("L");
            XmlAttribute attrp = doc.CreateAttribute("P");
            attrp.Value = rdpport + "";
            newele.Attributes.Append(attrp);
            XmlAttribute attrn = doc.CreateAttribute("N");
            attrn.Value = "127.0.0.1";
            newele.Attributes.Append(attrn);
            node.AppendChild(newele);

            node = doc.SelectSingleNode("//rdptype");
            if (node != null)
            {
                int.TryParse(node.InnerText, out m_rdpviewtype);                
            }

            try
            {
                if (m_rdpviewtype == 1)
                {
                    if (m_viewer2.Connected != 0)
                    {
                        m_viewer2.Disconnect();
                    }
                    m_viewer.Hide();
                    m_viewer2.Show();
                    m_viewer2.Server = "127.0.0.1";
                    m_viewer2.AdvancedSettings8.EnableCredSspSupport = true;
                    m_viewer2.AdvancedSettings8.RDPPort = rdpport;
                    m_viewer2.AdvancedSettings8.RedirectDevices = false;
                    m_viewer2.AdvancedSettings8.RedirectDrives = false;
                    m_viewer2.AdvancedSettings8.RedirectPorts = false;
                    m_viewer2.AdvancedSettings8.RedirectPOSDevices = false;
                    m_viewer2.AdvancedSettings8.RedirectPrinters = false;
                    m_viewer2.AdvancedSettings8.RedirectSmartCards = false;
                    m_viewer2.Connect();
                    m_viewer2.Update();
                }
                else
                {
                    m_viewer.Show();
                    m_viewer2.Hide();
                    string connstr = doc.InnerXml;
                    m_viewer.Connect(connstr, "controller", "");
                }
            }
            catch (Exception e)
            {
                m_RdpListener.Stop();
                m_RdpListener = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <param name="strcode"></param>
        /// <param name="m_closecallack"></param>
        /// <returns></returns>
        public bool Connect(string hostname, int port, string strcode, AsyncCloseCallback m_closecallack, AxRDPCOMAPILib.AxRDPViewer viewer, AxMSTSCLib.AxMsRdpClient7NotSafeForScripting viewer2)
        {
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
                if (!RdConnect(strcode))
                {
                    Logger.Trace("OnClose");
                    OnClose(null);
                    return false;
                }
                m_qos.SetQOS(256 * 1024, 128 * 1024);
                m_viewer = viewer;
                m_viewer2 = viewer2;
                if (!OpenRDPView())
                {
                    Logger.Trace("OnClose");
                    OnClose(null);
                    return false;
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

        public bool DisConnect()
        {
            Logger.Trace("OnClose");
            OnClose(null);
            return true;
        }

        /// <summary>
        /// RDP连接事件
        /// </summary>
        /// <param name="ar"></param>
        private void OnRdpConnect(IAsyncResult ar)
        {
            if (m_rdpclient != null)
            {
                try
                {
                    m_rdpclient.Close();
                }
                catch (Exception e)
                {

                }
            }
            if (m_RdpListener == null)
            {
                return;
            }
            try
            {
                m_rdpclient = m_RdpListener.EndAcceptTcpClient(ar);
            }
            catch (Exception e)
            {
                Logger.Trace(e);
                return;
            }

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

            try
            {
                m_rdpbuffer = null;
                m_rdpbuffer = new byte[RECV_BUFF_SIZE];
                m_rdpclient.GetStream().BeginRead(m_rdpbuffer, 0, m_rdpbuffer.Length, OnRecvRdpData, m_rdpclient);
            }
            catch (Exception e)
            {
                Logger.Trace(e);
            }
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
                recv = m_rdpclient.GetStream().EndRead(ar);
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

            // continue listening for tcp datagram packets 
            try
            {
                m_rdpclient.GetStream().BeginRead(m_rdpbuffer, 0, m_rdpbuffer.Length, OnRecvRdpData, m_rdpclient);
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
            catch (Exception e)
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

            if (m_rdpclient == null)
            {
                Logger.Trace("OnClose");
                OnClose(null);
                return;
            }

            try
            {
                NetworkStream stream = m_rdpclient.GetStream();
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
            try
            {
                if (m_rdpviewtype == 1)
                {
                    if (m_viewer2 != null)
                    {
                        m_viewer2.Disconnect();
                        m_viewer2 = null;
                    }
                }
                else
                {
                    if (m_viewer != null)
                    {
                        m_viewer.Disconnect();
                        m_viewer = null;
                    }
                }
            }
            catch (Exception e)
            {


            }
            try
            {
                if (m_tcpclient != null)
                {
                    TcpClient tcpclient = m_tcpclient;
                    m_tcpclient = null;
                    tcpclient.Close();
                }
            }catch(Exception e)
            {

            }

            try
            {
                if (m_rdpclient != null)
                {
                    TcpClient rdpclient = m_rdpclient;
                    m_rdpclient = null;
                    rdpclient.Close();
                }
            }
            catch (Exception e)
            {

            }

            try
            {
                if (m_RdpListener != null)
                {
                    TcpListener RdpListener = m_RdpListener;
                    m_RdpListener = null;
                    RdpListener.Stop();
                }
            }
            catch (Exception e)
            {

            }

            if (m_closecallack != null)
            {
                AsyncCloseCallback closecallack = m_closecallack;
                m_closecallack = null;
                closecallack();
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
                byte[] data = new byte[len+4];
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
        /// 发送连接码
        /// </summary>
        /// <param name="strcode"></param>
        /// <returns></returns>
        public bool RdConnect(string strcode)
        {
            byte[] data = m_protocol.C2S_RdConnect(strcode);
            WriteData(data);

            int cmd = 0;
            data = ReadData();
            data = m_protocol.ParseCmd(data, ref cmd);
            if (cmd != (int)Protocol.CMD.S2C_RDCONNECT)
            {
                return false;
            }
            m_rdpConnectionString = System.Text.UTF8Encoding.UTF8.GetString(data);

            //---
            cmd = 0;
            data = m_protocol.C2C_RdpStart(strcode);
            WriteData(data);
            data = ReadData();
            data = m_protocol.ParseCmd(data, ref cmd);
            if (cmd != (int)Protocol.CMD.C2C_RDPSTARTRES)
            {
                return false;
            }
            m_startres = System.Text.UTF8Encoding.UTF8.GetString(data);
            if (m_startres != "OK")
            {
                return false;
            }
            return true;
        }
    }
}
