#if __UNIFIED__ || __IOS__ || __ANDROID__

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using pbXNet;

namespace pbXSecurity
{
    public partial class AesCryptographer : ICryptographer
    {
        public byte[] GenerateKey(byte[] pwd, byte[] salt, int length = 32)
        {
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(pwd, salt)
            {
                IterationCount = 10000
            };
            return pdb.GetBytes(length);
        }

        public byte[] GenerateIV(int length = 16)
        {
            AesManaged objAlg = new AesManaged();
            objAlg.GenerateIV();
            return objAlg.IV;
        }

        public byte[] Encrypt(byte[] msg, byte[] key, byte[] iv)
        {
            // algoritm
            AesManaged objAlg = new AesManaged();

            // prepare
            objAlg.Key = key;
            objAlg.IV = iv;

            // encrypt using streams
            using (MemoryStream sMsgEncrypted = new MemoryStream())
            {
                CryptoStream csEncrypt = new CryptoStream(sMsgEncrypted, objAlg.CreateEncryptor(), CryptoStreamMode.Write);
                try
                {
                    csEncrypt.Write(msg, 0, msg.Length);
                    csEncrypt.Close();
                    return sMsgEncrypted.ToArray();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AesCryptographer: Encrypt: error: {ex.Message}");
                    return new byte[0];
                }
            }
        }

        public byte[] Decrypt(byte[] msg, byte[] key, byte[] iv)
        {
            // algoritm
            AesManaged objAlg = new AesManaged();

            // prepare
            objAlg.Key = key;
            objAlg.IV = iv;

            // decrypt using streams
            using (MemoryStream sMsgDecrypted = new MemoryStream())
            {
                CryptoStream csDecrypt = new CryptoStream(sMsgDecrypted, objAlg.CreateDecryptor(), CryptoStreamMode.Write);
                try
                {
                    csDecrypt.Write(msg, 0, msg.Length);
                    csDecrypt.Close();
                    return sMsgDecrypted.ToArray();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AesCryptographer: Decrypt: error: {ex.Message}");
                    return new byte[0];
                }
            }
        }
    }
}

#endif