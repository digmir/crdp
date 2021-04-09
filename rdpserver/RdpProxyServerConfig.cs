using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rdpserver
{
    /// <summary>
    /// 配置项
    /// </summary>
    public partial class RdpProxyServer : IServer 
    {
        /// <summary>
        /// 客户端监听端口
        /// </summary>
        public ushort CLIENT_PORT { get; set; }

        /// <summary>
        /// 控制端监听端口
        /// </summary>
        public ushort CONTROLLER_PORT { get; set; }

        /// <summary>
        /// 密钥
        /// </summary>
        public string ENCRYPT_KEY { get; set; }
    }
}
