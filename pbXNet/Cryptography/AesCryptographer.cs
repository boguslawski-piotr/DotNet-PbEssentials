using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace pbXNet
{
	public class AesCryptographer : ICryptographer
	{
		// Default Aes settings:
		// .NET 4.6.1 Windows 10, .NET Core 1.1 Windows 10
		// Xamarin.iOS, Xamarin.macOS, Xamarin.Android
		//
		//   blockSize: 128
		//   keySize:	256
		//   mode: CBC
		//   padding: PKCS7

		static readonly int _blockSize = 128;
		static readonly int _keySize = 256;
		static readonly CipherMode _mode = CipherMode.CBC;
		static readonly PaddingMode _padding = PaddingMode.PKCS7;

		static readonly byte[] _magic = { 0x33, 0xdd, 0x77, 0x22 };
		static readonly byte _block128 = 0x01;
		static readonly byte _key256 = 0x01;
		static readonly byte _modeCbc = 0x01;
		static readonly byte _paddingPkcs7 = 0x01;
		static readonly int _signatureSize = 8;

		Aes _algImpl
		{
			get {
				Aes aes = Aes.Create();

				aes.BlockSize = _blockSize;
				aes.KeySize = _keySize;
				aes.Mode = _mode;
				aes.Padding = _padding;

				if (aes.BlockSize != _blockSize || aes.KeySize != _keySize || aes.Mode != _mode || aes.Padding != _padding)
					throw new CryptographicException(T.Localized("AES_UnsupportedParams"));

				return aes;
			}
		}

		public IByteBuffer GenerateKey(IPassword pwd, IByteBuffer salt)
		{
			Check.Null(pwd, nameof(pwd));
			Check.Null(salt, nameof(salt));

			Aes alg = _algImpl;
			Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(pwd.GetBytes(), salt.GetBytes(), 10000);
			SecureBuffer key = new SecureBuffer(pdb.GetBytes(alg.KeySize / 8), true);
			pwd.DisposeBytes();
			salt.DisposeBytes();
			return key;
		}

		public IByteBuffer GenerateIV()
		{
			Aes alg = _algImpl;
			alg.GenerateIV();
			return new SecureBuffer(alg.IV);
		}

		public ByteBuffer Encrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv)
		{
			Check.Null(msg, nameof(msg));
			Check.Null(key, nameof(key));
			Check.Null(iv, nameof(iv));

			Aes alg = _algImpl;
			ByteBuffer emsg = Transform(msg.GetBytes(), msg.Length, key, iv, alg, true);
			msg.DisposeBytes();

			List<byte> signature = new List<byte>(_signatureSize);
			signature.AddRange(_magic);
			signature.Add(_block128);
			signature.Add(_key256);
			signature.Add(_modeCbc);
			signature.Add(_paddingPkcs7);

			emsg.Append(signature.ToArray());
			return emsg;
		}

		public ByteBuffer Decrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv)
		{
			Check.Null(msg, nameof(msg));
			Check.Null(key, nameof(key));
			Check.Null(iv, nameof(iv));
			Check.True(msg.Length > _signatureSize, T.Localized("AES_IncrorrectFormat"), nameof(msg));

			try
			{
				byte[] amsg = msg.GetBytes();

				int si = amsg.Length - _signatureSize;
				if (amsg[si + 0] != _magic[0] ||
					amsg[si + 1] != _magic[1] ||
					amsg[si + 2] != _magic[2] ||
					amsg[si + 3] != _magic[3] ||
					amsg[si + 4] != _block128 ||
					amsg[si + 5] != _key256 ||
					amsg[si + 6] != _modeCbc ||
					amsg[si + 7] != _paddingPkcs7)
					throw new ArgumentException(T.Localized("AES_IncrorrectFormat"), nameof(msg));

				Aes alg = _algImpl;
				return Transform(amsg, si, key, iv, alg, false);
			}
			finally
			{
				msg.DisposeBytes();
			}
		}

		ByteBuffer Transform(byte[] msg, int length, IByteBuffer key, IByteBuffer iv, Aes alg, bool encrypt)
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
						cs.Write(msg, 0, length);
					}

					return new ByteBuffer(msgTransformed.ToArray());
				}
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				throw ex;
			}
			finally
			{
				key.DisposeBytes();
				iv.DisposeBytes();
			}
		}

		public void Dispose()
		{
		}
	}
}
