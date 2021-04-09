using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace rdpserver
{
    /// <summary>
    /// 连接码管理
    /// </summary>
    class ConnCode
    {
        static Hashtable codehash = new Hashtable();
        static Hashtable paramhash = new Hashtable();
        static Hashtable pwdhash = new Hashtable();

        static public string GetCode(TCPClientState state, string param, string oldcode, string pwd, string xml)
        {
            string strcode;
            if(string.IsNullOrWhiteSpace(oldcode))
            {
                int code = param.GetHashCode();
                char[] CODELIST = "abcdefghijkmnpqrstuvwxyz0123456789".ToCharArray();

                string pre = "";
                byte[] coderarry = BitConverter.GetBytes(code);
                for (int i = 0; i < coderarry.Length; i++)
                {
                    pre += CODELIST[coderarry[i] % CODELIST.Length];
                }

                strcode = pre + CODELIST[Math.Abs(code) % CODELIST.Length];
                while (paramhash[strcode] != null && ((string)paramhash[strcode]) != xml)
                {
                    code++;
                    strcode = pre + CODELIST[Math.Abs(code) % CODELIST.Length];
                }
            }
            else
            {
                strcode = oldcode;
            }
            codehash[strcode] = state;
            paramhash[strcode] = xml;
            pwdhash[strcode] = pwd;
            return strcode;
        }

        static public TCPClientState GetState(string strcode, out string param, out string pwd)
        {
            param = (string)paramhash[strcode];
            pwd = (string)pwdhash[strcode];
            object obj = codehash[strcode];
            if (obj == null)
            {
                return null;
            }
            return (TCPClientState)obj;
        }
        static public void DelState(string strcode)
        {
            paramhash[strcode] = null;
            codehash[strcode] = null;
            pwdhash[strcode] = null;
        }
    }
}
