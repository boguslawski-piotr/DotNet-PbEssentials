using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace pbXNet
{
	public partial class AesCryptographer : ICryptographer
	{
		public IByteBuffer GenerateKey(IPassword pwd, IByteBuffer salt, int length = 32)
		{
			PasswordDeriveBytes pdb = new PasswordDeriveBytes(pwd.GetBytes(), salt.GetBytes())
			{
				IterationCount = 10000
			};

			SecureBuffer key = new SecureBuffer(pdb.GetBytes(length), true);
			pwd.DisposeBytes();
			salt.DisposeBytes();

			return key;
		}

		public IByteBuffer GenerateIV(int length = 16)
		{
			AesManaged objAlg = new AesManaged();
			objAlg.GenerateIV();
			return new SecureBuffer(objAlg.IV);
		}

		public ByteBuffer Encrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv)
		{
			AesManaged objAlg = new AesManaged()
			{
				Key = key.GetBytes(),
				IV = iv.GetBytes()
			};

			try
			{
				using (MemoryStream sMsgEncrypted = new MemoryStream())
				{
					using (CryptoStream csEncrypt = new CryptoStream(sMsgEncrypted, objAlg.CreateEncryptor(), CryptoStreamMode.Write))
					{
						try
						{
							csEncrypt.Write(msg.GetBytes(), 0, msg.Length);
							csEncrypt.Close();
							return new ByteBuffer(sMsgEncrypted.ToArray());
						}
						catch (Exception ex)
						{
							Log.E(ex.Message, this);
							LastEx = ex;
							return new ByteBuffer();
						}
					}
				}
			}
			finally
			{
				key.DisposeBytes();
				iv.DisposeBytes();
				msg.DisposeBytes();
			}
		}

		public ByteBuffer Decrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv)
		{
			AesManaged objAlg = new AesManaged()
			{
				Key = key.GetBytes(),
				IV = iv.GetBytes()
			};

			try
			{
				using (MemoryStream sMsgDecrypted = new MemoryStream())
				{
					using (CryptoStream csDecrypt = new CryptoStream(sMsgDecrypted, objAlg.CreateDecryptor(), CryptoStreamMode.Write))
					{
						try
						{
							csDecrypt.Write(msg.GetBytes(), 0, msg.Length);
							csDecrypt.Close();
							return new ByteBuffer(sMsgDecrypted.ToArray());
						}
						catch (Exception ex)
						{
							Log.E(ex.Message, this);
							LastEx = ex;
							return new ByteBuffer();
						}
					}
				}
			}
			finally
			{
				key.DisposeBytes();
				iv.DisposeBytes();
				msg.DisposeBytes();
			}
		}

		public void Dispose()
		{
		}
	}
}
