#if __UNIFIED__ || __IOS__ || __ANDROID__

using System;
using System.IO;
using System.Security.Cryptography;

using pbXNet;

namespace pbXSecurity
{
    public partial class AesCryptographer : ICryptographer
    {
        public byte[] GenerateKey(byte[] pwd, byte[] salt, int length = 32)
        {
            const int iterations = 10000;

            using (Rfc2898DeriveBytes d = new Rfc2898DeriveBytes(pwd, salt, iterations))
            {
                return d.GetBytes(length);
            }
        }

        public byte[] GenerateIV(int length = 16)
        {
            AesManaged /*RijndaelManaged*/ objAlg = new AesManaged() /*RijndaelManaged()*/;
            objAlg.GenerateIV();
            return objAlg.IV;
        }

        //

        public byte[] Encrypt(byte[] msg, byte[] key, byte[] iv)
        {
            // algoritm
            AesManaged /*RijndaelManaged*/ objAlg = new AesManaged() /*RijndaelManaged()*/;

            // prepare
            ICryptoTransform encryptor = objAlg.CreateEncryptor(key, iv);

            // encrypt using streams
            using (MemoryStream sMsgEncrypted = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(sMsgEncrypted, encryptor, CryptoStreamMode.Write))
                {
                    using (MemoryStream sMsg = new MemoryStream(msg))
                    {
                        sMsg.CopyTo(csEncrypt);
                    }

                    byte[] msgEncrypted = sMsgEncrypted.ToArray();
                    return msgEncrypted;
                }
            }
        }

        public byte[] Decrypt(byte[] msg, byte[] key, byte[] iv)
        {
            // algoritm
            AesManaged /*RijndaelManaged*/ objAlg = new AesManaged() /*RijndaelManaged()*/;

            // prepare
            ICryptoTransform decryptor = objAlg.CreateDecryptor(key, iv);

            // decrypt using streams
            using (MemoryStream sMsg = new MemoryStream(msg))
            {
                using (CryptoStream csDecrypt = new CryptoStream(sMsg, decryptor, CryptoStreamMode.Read))
                {
                    using (MemoryStream sMsgDecrypted = new MemoryStream())
                    {
                        csDecrypt.CopyTo(sMsgDecrypted);
                        byte[] msgDecrypted = sMsgDecrypted.ToArray();
                        return msgDecrypted;
                    }
                }
            }
        }
    }
}

#endif