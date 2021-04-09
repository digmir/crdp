using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rdpserver
{
    /// <summary>
    /// 服务接口
    /// </summary>
    interface IServer
    {
        /// <summary>
        /// 运行服务
        /// </summary>
        void Run();
    }
}
