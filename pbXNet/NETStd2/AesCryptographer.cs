using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace pbXNet
{
	public partial class AesCryptographer : ICryptographer
	{
		//Aes _algImpl => new AesManaged();
		Aes _algImpl => Aes.Create();

		public IByteBuffer GenerateKey(IPassword pwd, IByteBuffer salt, int length = 32)
		{
			Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(pwd.GetBytes(), salt.GetBytes(), 10000);
			SecureBuffer key = new SecureBuffer(pdb.GetBytes(length), true);
			pwd.DisposeBytes();
			salt.DisposeBytes();
			return key;
		}

		public IByteBuffer GenerateIV(int length = 16)
		{
			Aes alg = _algImpl;
			alg.GenerateIV();
			return new SecureBuffer(alg.IV);
		}

		public ByteBuffer Encrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv)
		{
			Aes alg = _algImpl;
			return Transform(msg, key, iv, alg, true);
		}

		public ByteBuffer Decrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv)
		{
			Aes alg = _algImpl;
			return Transform(msg, key, iv, alg, false);
		}

		ByteBuffer Transform(IByteBuffer msg, IByteBuffer key, IByteBuffer iv, Aes alg, bool encrypt)
		{
			try
			{
				alg.Key = key.GetBytes();
				alg.IV = iv.GetBytes();
				ICryptoTransform transform = encrypt ? alg.CreateEncryptor() : alg.CreateDecryptor();

				using (MemoryStream msgTransformed = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(msgTransformed, transform, CryptoStreamMode.Write))
					{
						cs.Write(msg.GetBytes(), 0, msg.Length);
					}

					return new ByteBuffer(msgTransformed.ToArray());
				}
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				return new ByteBuffer();
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
