using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace rdpserver
{
    enum CLIENTTYPE
    {
        CLIENT = 1, CONTROLLER = 2
    }

    /// <summary>
    /// 客户端连接状态值
    /// </summary>
    class TCPClientState
    {
        /// <summary>  
        /// 客户端连接
        /// </summary>  
        public TcpClient TcpClient { get; private set; }

        /// <summary>  
        /// 客户端连接
        /// </summary>  
        public TCPClientState PeerTcpClient { get; set; }

        /// <summary>  
        /// 获取缓冲区  
        /// </summary>  
        public byte[] Buffer { get; private set; }

        /// <summary>  
        /// 类型  
        /// </summary>  
        public CLIENTTYPE ClientType { get; set; }

        /// <summary>  
        /// 连接序号  
        /// </summary>  
        public string conncode { get; set; }

        /// <summary>  
        /// 连接时间  
        /// </summary>  
        public DateTime? ConnectTime { get; set; }

        /// <summary>  
        /// 获取网络流  
        /// </summary>  
        public NetworkStream NetworkStream
        {
            get
            {
                try
                {
                    return TcpClient.GetStream();
                }
                catch (Exception e)
                {
                    //Logger.Trace(e);
                    return null;
                }
            }
        }

        /// <summary>
        /// 监听对象
        /// </summary>
        public TcpListener listener { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="server"></param>
        /// <param name="tcpClient"></param>
        /// <param name="buffer"></param>
        public TCPClientState(TcpListener server, TcpClient tcpClient, byte[] buffer)
        {
            if (server == null)
                throw new ArgumentNullException("server");
            if (tcpClient == null)
                throw new ArgumentNullException("tcpClient");
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            this.listener = server;
            this.TcpClient = tcpClient;
            this.Buffer = buffer;
            this.ConnectTime = DateTime.Now;
        }

        /// <summary>  
        /// 关闭  
        /// </summary>  
        public void Close()
        {
            //关闭数据的接受和发送  
            try
            {
                if (this.TcpClient == null)
                {
                    return;
                }

                if (ClientType == CLIENTTYPE.CLIENT)
                {
                    Logger.Trace("Client Close code=" + conncode);
                    if (conncode != null && conncode != "")
                    {
                        ConnCode.DelState(conncode);
                    }
                }
                else if (ClientType == CLIENTTYPE.CONTROLLER)
                {
                    Logger.Trace("Controller Close code=" + conncode);
                }
                TcpClient myclient = this.TcpClient;
                TCPClientState peer = this.PeerTcpClient;
                this.TcpClient = null;
                this.PeerTcpClient = null;

                if (peer != null)
                {
                    peer.PeerTcpClient = null;
                    peer.Close();
                }
                if (myclient != null)
                {
                    if (myclient.Connected)
                    {
                        myclient.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Trace(e);
            }
            Buffer = null;
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        public byte[] ReadData(IAsyncResult ar)
        {
            NetworkStream stream = NetworkStream;
            if (stream == null)
            {
                return null;
            }
            int recv = 0;
            try
            {
                recv = stream.EndRead(ar);
            }
            catch
            {
                recv = 0;
            }

            if (recv <= 0)
            {
                // connection has been closed  
                Close();
                return null;
            }

            // received byte and trigger event notification  
            byte[] buff = new byte[recv];
            Array.Copy(this.Buffer, 0, buff, 0, recv);
            return buff;
        }

        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="data"></param>
        public void WriteData(byte[] data)
        {
            try
            {
                NetworkStream stream = NetworkStream;
                if (stream == null)
                {
                    Close();
                    return;
                }
                stream.BeginWrite(data, 0, data.Length, WriteDataEnd, this);
            }
            catch (Exception e)
            {
                Logger.Trace(e);
                Close();
            }
        }

        private void WriteDataEnd(IAsyncResult ar)
        {
            NetworkStream stream = NetworkStream;
            if (stream == null)
            {
                return;
            }
            try
            {
                stream.EndWrite(ar);
            }
            catch (Exception e)
            {
                Logger.Trace(e);
                Close();
                return;
            }
        }

    }
}
