using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace rdpcommon
{
    class Protocol
    {
        public const string SERVER_HOST = "rdpserver.digmir.com";
        public const int CLIENT_PORT = 9833;
        public const int CONTROLLER_PORT = 9834;

        public enum CMD
        {
            C2S_HANDSHAKE = 1,
            S2C_HANDSHAKE = 2,
            C2S_RDCONNECT = 3,
            S2C_RDCONNECT = 4,
            C2C_RDPSTART = 5,
            C2C_RDPSTARTRES = 6,
        }

        public const string ENCRYPT_KEY = "crdp-encrypt-key";
        RijndaelManaged m_aes = new RijndaelManaged();

        public Protocol(string ParamEncryptKey)
        {
            string EncryptKey = ParamEncryptKey;
            if (string.IsNullOrEmpty(EncryptKey))
            {
                EncryptKey = ENCRYPT_KEY;
            }
            EncryptKey = EncryptKey.PadRight(32);
            m_aes.Key = UTF8Encoding.UTF8.GetBytes(EncryptKey);
            m_aes.Mode = CipherMode.ECB;
            m_aes.Padding = PaddingMode.PKCS7;
        }

        private byte[] BuildPack(CMD c, byte[] data)
        {
            byte[] cmd = BitConverter.GetBytes((uint)c);
            byte[] src = new byte[cmd.Length + data.Length];
            Array.Copy(cmd, 0, src, 0, cmd.Length);
            Array.Copy(data, 0, src, cmd.Length, data.Length);

            byte[] result;
            try
            {
                ICryptoTransform cTransform = m_aes.CreateEncryptor();
                result = cTransform.TransformFinalBlock(src, 0, src.Length);
            }
            catch(Exception e)
            {
                Logger.Trace(e);
                return null;
            }

            byte[] ret = new byte[result.Length + 4];
            Array.Copy(BitConverter.GetBytes(result.Length), ret, 4);
            Array.Copy(result, 0, ret, 4, result.Length);
            return ret;
        }
        

        /// <summary>
        /// 解析命令数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="datalen"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public byte[] ParseCmd(byte[] data,int datalen, ref int cmd)
        {
            if (datalen < 8 || datalen > 1024 * 1024 * 8)
            {
                return null;
            }
            int len = BitConverter.ToInt32(data, 0);
            if (len > datalen-4)
            {
                return null;
            }

            byte[] result = null;
            try
            {
                ICryptoTransform cTransform = m_aes.CreateDecryptor();
                result = cTransform.TransformFinalBlock(data, 4, len);
            }
            catch
            {
                return null;
            }
            if (result == null)
            {
                return null;
            }

            cmd = BitConverter.ToInt32(result, 0);
            byte[] ret = new byte[result.Length - 4];
            Array.Copy(result, 4, ret, 0, result.Length - 4);
            return ret;
        }

        /// <summary>
        /// 解析命令数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public byte[] ParseCmd(byte[] data, ref int cmd)
        {
            return ParseCmd(data, data.Length, ref cmd);
        }
        public byte[] C2S_Handshake(string param)
        {
            return BuildPack(CMD.C2S_HANDSHAKE, System.Text.UTF8Encoding.UTF8.GetBytes(param));
        }
        public byte[] C2S_RdConnect(string strcode)
        {
            if (strcode == null || strcode == "")
            {
                return null;
            }
            byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(strcode);
            return BuildPack(CMD.C2S_RDCONNECT, data);
        }
        public byte[] S2C_Handshake(string strcode)
        {
            if (strcode == null || strcode == "")
            {
                return null;
            }
            byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(strcode);
            return BuildPack(CMD.S2C_HANDSHAKE, data);
        }
        public byte[] S2C_RdConnect(string msg)
        {
            if (msg == null || msg == "")
            {
                return null;
            }
            byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(msg);
            return BuildPack(CMD.S2C_RDCONNECT, data);
        }
        public byte[] C2C_RdpStart(string strcode)
        {
            if (strcode == null || strcode == "")
            {
                return null;
            }
            byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(strcode);
            return BuildPack(CMD.C2C_RDPSTART, data);
        }
        public byte[] C2C_RdpStartRes(string res)
        {
            if (res == null || res == "")
            {
                res = "OK";
            }
            byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(res);
            return BuildPack(CMD.C2C_RDPSTARTRES, data);
        }
    }
}
