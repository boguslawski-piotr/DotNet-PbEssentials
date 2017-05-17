using System;
using System.IO;
using System.Threading.Tasks;

using pbXNet;

#if WINDOWS_UWP
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
#endif

#if __UNIFIED__ || __ANDROID__
using System.Security.Cryptography;
#endif

namespace pbXSecurity
{
    public class Cryptographer : ICryptographer
    {
        //

        public string Obfuscate(string d)
        {
            ICompressor compressor = new Compressor();
            d = compressor.Compress(d);
            using (MemoryStream s = ConvertEx.ToMemoryStream(d))
            {
                byte[] a = s.ToArray();
                return Convert.ToBase64String(a);
            }
        }

        async public Task<string> ObfuscateAsync(string d)
            => await Task.Run(() => Obfuscate(d));

        public string DeObfuscate(string d)
        {
            byte[] a = Convert.FromBase64String(d);
            using (MemoryStream s = new MemoryStream(a))
            {
                d = ConvertEx.ToString(s);
                ICompressor compressor = new Compressor();
                return compressor.Decompress(d);
            }
        }

        async public Task<string> DeObfuscateAsync(string d)
            => await Task.Run(() => DeObfuscate(d));

        //

        public byte[] GenerateKey(byte[] pwd, byte[] salt, int length = 32)
        {
            const int iterations = 10000;

            #region UWP
#if WINDOWS_UWP
            KeyDerivationAlgorithmProvider objKdfProv = KeyDerivationAlgorithmProvider.OpenAlgorithm(KeyDerivationAlgorithmNames.Pbkdf2Sha1);
            IBuffer buffPwd = CryptographicBuffer.CreateFromByteArray(pwd);
            IBuffer buffSalt = CryptographicBuffer.CreateFromByteArray(salt);
            KeyDerivationParameters pbkdf2Params = KeyDerivationParameters.BuildForPbkdf2(buffSalt, iterations);
            CryptographicKey keyOriginal = objKdfProv.CreateKey(buffPwd);
            IBuffer buffKey = CryptographicEngine.DeriveKeyMaterial(keyOriginal, pbkdf2Params, (uint)length);
            return IBufferToByteArray(buffKey);
#endif
            #endregion
            #region IOS ANDROID
#if __UNIFIED__ || __ANDROID__
            using (Rfc2898DeriveBytes d = new Rfc2898DeriveBytes(pwd, salt, iterations))
            {
                return d.GetBytes(length);
            }
#endif
            #endregion
        }

        public byte[] GenerateIV(int length = 16)
        {
            #region UWP
#if WINDOWS_UWP
            IBuffer buff = CryptographicBuffer.GenerateRandom((uint)length);
            return IBufferToByteArray(buff);
#endif
            #endregion
            #region IOS ANDROID
#if __UNIFIED__ || __ANDROID__
            AesManaged /*RijndaelManaged*/ objAlg = new AesManaged() /*RijndaelManaged()*/;
            objAlg.GenerateIV();
            return objAlg.IV;
#endif
            #endregion
        }

        //

        public byte[] Encrypt(byte[] msg, byte[] key, byte[] iv)
        {
            #region UWP
#if WINDOWS_UWP
            // algoritm
            SymmetricKeyAlgorithmProvider objAlg = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);

            // prepare
            IBuffer buffKey = CryptographicBuffer.CreateFromByteArray(key);
            CryptographicKey ckey = objAlg.CreateSymmetricKey(buffKey);
            IBuffer buffMsg = CryptographicBuffer.CreateFromByteArray(msg);
            IBuffer buffIv = CryptographicBuffer.CreateFromByteArray(iv);

            // encrypt
            IBuffer buffMsgEncrypted = CryptographicEngine.Encrypt(ckey, buffMsg, buffIv);
            return IBufferToByteArray(buffMsgEncrypted);
#endif
            #endregion
            #region IOS ANDROID
#if __UNIFIED__ || __ANDROID__
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
#endif
            #endregion
        }

        async public Task<byte[]> EncryptAsync(byte[] msg, byte[] key, byte[] iv)
            => await Task.Run(() => Encrypt(msg, key, iv));

        public byte[] Decrypt(byte[] msg, byte[] key, byte[] iv)
        {
            #region UWP
#if WINDOWS_UWP
            // algoritm
            SymmetricKeyAlgorithmProvider objAlg = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);

            // prepare
            IBuffer buffKey = CryptographicBuffer.CreateFromByteArray(key);
            CryptographicKey ckey = objAlg.CreateSymmetricKey(buffKey);
            IBuffer buffMsg = CryptographicBuffer.CreateFromByteArray(msg);
            IBuffer buffIv = CryptographicBuffer.CreateFromByteArray(iv);

            // decrypt
            IBuffer buffMsgDecrypted = CryptographicEngine.Decrypt(ckey, buffMsg, buffIv);
            return IBufferToByteArray(buffMsgDecrypted);
#endif
            #endregion
            #region IOS ANDROID
#if __UNIFIED__ || __ANDROID__
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
#endif
            #endregion
        }

        async public Task<byte[]> DecryptAsync(byte[] msg, byte[] key, byte[] iv)
            => await Task.Run(() => Decrypt(msg, key, iv));

        //
        #region Tools UWP
#if WINDOWS_UWP

        //public static string IBufferToString(IBuffer buf)
        //{
        //    byte[] rawBytes = new byte[buf.Length];
        //    using (var reader = DataReader.FromBuffer(buf))
        //    {
        //        reader.ReadBytes(rawBytes);
        //    }
        //    var encoding = Encoding.UTF8;
        //    return encoding.GetString(rawBytes, 0, rawBytes.Length);
        //}

        private static byte[] IBufferToByteArray(IBuffer buf)
        {
            byte[] rawBytes = new byte[buf.Length];
            using (var reader = DataReader.FromBuffer(buf))
            {
                reader.ReadBytes(rawBytes);
            }
            return rawBytes;
        }

#endif
        #endregion
    }
}
