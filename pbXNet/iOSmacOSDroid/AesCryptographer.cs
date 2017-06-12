#if __UNIFIED__ || __ANDROID__

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace pbXNet
{
	public partial class AesCryptographer : ICryptographer
	{
		public byte[] GenerateKey(Password pwd, byte[] salt, int length = 32)
		{
			PasswordDeriveBytes pdb = new PasswordDeriveBytes(pwd, salt)
			{
				IterationCount = 10000
			};
			byte[] key = pdb.GetBytes(length);
			pwd.Clear(false);
			return key;
		}

		public byte[] GenerateIV(int length = 16)
		{
			AesManaged objAlg = new AesManaged();
			objAlg.GenerateIV();
			return objAlg.IV;
		}

		public byte[] Encrypt(byte[] msg, byte[] key, byte[] iv)
		{
			AesManaged objAlg = new AesManaged()
			{
				Key = key,
				IV = iv
			};

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
					Log.E(ex.Message, this);
					return new byte[0];
				}
			}
		}

		public byte[] Decrypt(byte[] msg, byte[] key, byte[] iv)
		{
			AesManaged objAlg = new AesManaged()
			{
				Key = key,
				IV = iv
			};

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
					Log.E(ex.Message, this);
					return new byte[0];
				}
			}
		}
	}
}

#endif