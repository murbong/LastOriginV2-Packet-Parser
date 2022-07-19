using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace PcapV2
{
    public class AESManager
    {

        private readonly Aes aesInstance;

        public static string Key => "";
        public static string IV => "";
        public AESManager()
        {
            aesInstance = Aes.Create();
        }

        public static byte[] Hex2Key(string str)
        {
            byte[] convertArr = new byte[str.Length / 2];
            for(var i = 0; i < convertArr.Length; i++)
            {
                convertArr[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);    
            }
            return convertArr;
        }

        public string Decrypt(byte[] byteArr)
        {
            var key = Hex2Key(Key);
            var iv = Hex2Key(IV);

            return DecryptBytes(byteArr,key,iv);
        }

        public byte[] Encrypt(string str)
        {

            var key = Hex2Key(Key);
            var iv = Hex2Key(IV);

            return EncryptString(str, key, iv);
        }
        private string DecryptBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {

            var cipher = aesInstance.CreateDecryptor(Key, IV);

            string str;

            using(var ms = new MemoryStream(cipherText))
            {
                using (var cs = new CryptoStream(ms, cipher, CryptoStreamMode.Read))
                {
                    using (var sr = new StreamReader(cs))
                    {
                        str = sr.ReadToEnd();
                    }
                }
            }
            return str;
        }

        private byte[] EncryptString(string plainText, byte[] Key, byte[] IV)
        {

            var cipher = aesInstance.CreateEncryptor(Key, IV);

            var arr = Encoding.UTF8.GetBytes(plainText);

            byte[] buffer;
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, cipher, CryptoStreamMode.Write))
                {
                    cs.Write(arr, 0, arr.Length);
                    cs.FlushFinalBlock();
                }
                buffer = ms.ToArray();
            }
            return buffer;
        }

    }
}
