using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rdpserver
{
    class Program
    {
        static void Main(string[] args)
        {
            //config
            string ClientPort = System.Configuration.ConfigurationManager.AppSettings.Get("ClientPort");
            string ControllerPort = System.Configuration.ConfigurationManager.AppSettings.Get("ControllerPort");
            string EncryptKey = System.Configuration.ConfigurationManager.AppSettings.Get("EncryptKey");

            ushort nClientPort = rdpcommon.Protocol.CLIENT_PORT;
            ushort nControllerPort = rdpcommon.Protocol.CONTROLLER_PORT;

            if (string.IsNullOrEmpty(ClientPort))
            {
                ushort.TryParse(ClientPort, out nClientPort);
            }
            if (string.IsNullOrEmpty(ControllerPort))
            {
                ushort.TryParse(ClientPort, out nControllerPort);
            }
            if (string.IsNullOrEmpty(EncryptKey))
            {
                EncryptKey = rdpcommon.Protocol.ENCRYPT_KEY;
            }

            RdpProxyServer rpserver = new RdpProxyServer();
            rpserver.CLIENT_PORT = nClientPort;
            rpserver.CONTROLLER_PORT = nControllerPort;
            rpserver.ENCRYPT_KEY = EncryptKey;

            rpserver.Run();


            Console.WriteLine("Press [q] to quit...");

            while (Console.ReadKey().KeyChar != 'q') { }
        }
    }
}
