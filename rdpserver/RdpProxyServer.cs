using rdpcommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace rdpserver
{
    /// <summary>
    /// RDP连接反向代理服务器
    /// 监听两个端口，一个端口用于客户端，一个端口用于控制端
    /// </summary>
    public partial class RdpProxyServer : IServer
    {
        /// <summary>
        /// 用于客户端连接
        /// </summary>
        TcpListener m_ClientListener;

        /// <summary>
        /// 用于控制端连接
        /// </summary>
        TcpListener m_ControllerListener;
        
        /// <summary>
        /// 连接列表
        /// </summary>
        List<TCPClientState> m_ClientList = new List<TCPClientState>();

        /// <summary>
        /// 网络协议
        /// </summary>
        Protocol m_protocol;

        /// <summary>
        /// 是否在运行状态
        /// </summary>
        bool m_bRun;

        int RECV_BUFF_SIZE = 1024 * 1024;

        /// <summary>
        /// 运行服务
        /// </summary>
        public void Run()
        {
            try
            {
                m_bRun = true;

                m_protocol = new Protocol(ENCRYPT_KEY);

                m_ClientListener = new TcpListener(IPAddress.Any, CLIENT_PORT);
                m_ClientListener.Start();
                m_ClientListener.BeginAcceptTcpClient(OnClientConnect, m_ClientListener);

                m_ControllerListener = new TcpListener(IPAddress.Any, CONTROLLER_PORT);
                m_ControllerListener.Start();
                m_ControllerListener.BeginAcceptTcpClient(OnControllerConnect, m_ControllerListener);
                Logger.Trace("CLIENT_PORT=" + CLIENT_PORT);
                Logger.Trace("CONTROLLER_PORT=" + CONTROLLER_PORT);
            }
            catch (Exception e)
            {
                Logger.Trace(e);
            }
        }

        /// <summary>
        /// 客户端连接事件
        /// </summary>
        /// <param name="ar"></param>
        private void OnClientConnect(IAsyncResult ar)
        {
            try
            {
                if(!m_bRun)
                {
                    return;
                }
                TcpListener listener = (TcpListener)ar.AsyncState;
                TcpClient client = listener.EndAcceptTcpClient(ar);
                m_ClientListener.BeginAcceptTcpClient(OnClientConnect, m_ClientListener);
                byte[] buffer = new byte[RECV_BUFF_SIZE];
                TCPClientState state = new TCPClientState(listener, client, buffer);
                state.ClientType = CLIENTTYPE.CLIENT;

                lock (m_ClientList)
                {
                    m_ClientList.Add(state);
                }
                client.GetStream().BeginRead(state.Buffer, 0, state.Buffer.Length, OnRecvClientData, state);
            }
            catch (Exception e)
            {
                Logger.Trace(e);
            }
        }

        /// <summary>
        /// 控制端连接事件
        /// </summary>
        /// <param name="ar"></param>
        private void OnControllerConnect(IAsyncResult ar)
        {
            try
            {
                if (!m_bRun)
                {
                    return;
                }
                TcpListener listener = (TcpListener)ar.AsyncState;
                TcpClient controller = listener.EndAcceptTcpClient(ar);
                m_ControllerListener.BeginAcceptTcpClient(OnControllerConnect, m_ControllerListener);
                byte[] buffer = new byte[RECV_BUFF_SIZE];
                TCPClientState state = new TCPClientState(listener, controller, buffer);
                state.ClientType = CLIENTTYPE.CONTROLLER;

                lock (m_ClientList)
                {
                    m_ClientList.Add(state);
                }
                controller.GetStream().BeginRead(state.Buffer, 0, state.Buffer.Length, OnRecvClientData, state);
            }
            catch (Exception e)
            {
                Logger.Trace(e);
            }
        }

        private bool OnClientData(TCPClientState state, byte[] buf)
        {
            if (state.conncode == null)
            {
                int cmd = 0;
                byte[] data = m_protocol.ParseCmd(buf, ref cmd);
                if (cmd != (int)Protocol.CMD.C2S_HANDSHAKE || data == null || data.Length == 0)
                {
                    return false;
                }
                string xml = System.Text.UTF8Encoding.UTF8.GetString(data);
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xml);
                    XmlNode node = doc.SelectSingleNode("//param");
                    string param = "";
                    XmlNode tmpnode = node.SelectSingleNode("name");
                    if (tmpnode != null)
                    {
                        param += tmpnode.InnerText;
                    }
                    tmpnode = node.SelectSingleNode("mac");
                    if (tmpnode != null)
                    {
                        param += "|" + tmpnode.InnerText;
                    }
                    XmlNode pwdnode = node.SelectSingleNode("pwd");
                    string pwd = "";
                    if (pwdnode != null)
                    {
                        pwd = pwdnode.InnerText;
                    }
                    XmlNode codenode = node.SelectSingleNode("code");
                    string code = "";
                    if (codenode != null)
                    {
                        code = codenode.InnerText;
                    }
                    node.ParentNode.RemoveChild(node);
                    state.conncode = ConnCode.GetCode(state, param, code, pwd, doc.InnerXml);
                    state.WriteData(m_protocol.S2C_Handshake(state.conncode));
                    Logger.Trace("Client Connect code=" + state.conncode);
                }
                catch (Exception e)
                {
                    Logger.Trace("xml=" + xml);
                    Logger.Trace(e);
                    return false;
                }
            }
            else
            {
                if (state.PeerTcpClient != null)
                {
                    try
                    {
                        state.PeerTcpClient.WriteData(buf);
                    }
                    catch (Exception e)
                    {
                        Logger.Trace(e);
                    }
                }
            }
            return true;
        }

        private bool OnControllerData(TCPClientState state, byte[] buf)
        {
            if (state.conncode == null)
            {
                int cmd = 0;
                byte[] data = m_protocol.ParseCmd(buf, ref cmd);
                if (cmd != (int)Protocol.CMD.C2S_RDCONNECT || data == null || data.Length == 0)
                {;
                    return false;
                }
                string sdata = System.Text.UTF8Encoding.UTF8.GetString(data);
                if (string.IsNullOrEmpty(sdata))
                {
                    return false;
                }
                string[] rdata = sdata.Split(new char[] { '|' }, 3);
                if (rdata == null || rdata.Length != 3)
                {
                    return false;
                }
                state.conncode = rdata[1];
                string param;
                string pwd;
                TCPClientState peer = ConnCode.GetState(state.conncode, out param, out pwd);
                if (peer == null)
                {
                    Logger.Trace("客户端未连接");
                    return false;
                }
                if (pwd != rdata[2])
                {
                    Logger.Trace("密码错误");
                    return false;
                }
                state.PeerTcpClient = peer;
                peer.PeerTcpClient = state;
                state.WriteData(m_protocol.S2C_RdConnect(param));
                Logger.Trace("Controller Connect code=" + state.conncode);
            }
            else
            {
                if (state.PeerTcpClient != null)
                {
                    try
                    {
                        state.PeerTcpClient.WriteData(buf);
                    }
                    catch (Exception e)
                    {
                        Logger.Trace(e);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 接收数据事件
        /// </summary>
        /// <param name="ar"></param>
        private void OnRecvClientData(IAsyncResult ar)
        {
            TCPClientState state = (TCPClientState)ar.AsyncState;
            try
            {
                if (!m_bRun)
                {
                    return;
                }

                byte[] buf = state.ReadData(ar);

                if (buf == null)
                {
                    // connection has been closed  
                    lock (m_ClientList)
                    {
                        m_ClientList.Remove(state);
                    }
                    state.Close();
                    return;
                }

                if (state.ClientType == CLIENTTYPE.CLIENT)
                {
                    if (!OnClientData(state, buf))
                    {
                        state.Close();
                        return;
                    }
                }
                else if (state.ClientType == CLIENTTYPE.CONTROLLER)
                {
                    if (!OnControllerData(state, buf))
                    {
                        state.Close();
                        return;
                    }
                }
                else
                {
                    state.Close();
                    return;
                }

                // continue listening for tcp datagram packets 

                state.NetworkStream.BeginRead(state.Buffer, 0, state.Buffer.Length, OnRecvClientData, state);
            }
            catch (Exception e)
            {
                Logger.Trace(e);
                state.Close();
            }
        }

    }
}
