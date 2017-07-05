using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace pbXNet
{
	public class RsaKeyPair : IAsymmetricCryptographerKeyPair
	{
		public AsymmetricCryptographerAlgoritm Algoritm => AsymmetricCryptographerAlgoritm.Rsa;

		//
		// String versions of Public and Private keys.
		//
		// All components are byte[] arrays converted to HexString ("ABC" => "414243")
		// with the '-' character used as a separator and with following order:
		//
		// D-DP-DQ-Exponent-InverseQ-Modulus-P-Q
		//
		// When component is/should be null use only separator.
		// For example the public key will look like this:
		//
		// ---010001--D483...C5AD9--
		//
		// Once built, the entire string is compressed using Obfuscator.Obfuscate.
		//

		public string Public { get; }
		public string Private { get; }

		public RSAParameters RsaPublic;
		public RSAParameters RsaPrivate;

		public static string Pack(RSAParameters rsap)
		{
			string d = rsap.D?.ToHexString();
			string dp = rsap.DP?.ToHexString();
			string dq = rsap.DQ?.ToHexString();
			string exponent = rsap.Exponent?.ToHexString();
			string inverseq = rsap.InverseQ?.ToHexString();
			string modulus = rsap.Modulus?.ToHexString();
			string p = rsap.P?.ToHexString();
			string q = rsap.Q?.ToHexString();
			StringBuilder key = new StringBuilder()
				.Append(d)
				.Append("-")
				.Append(dp)
				.Append("-")
				.Append(dq)
				.Append("-")
				.Append(exponent)
				.Append("-")
				.Append(inverseq)
				.Append("-")
				.Append(modulus)
				.Append("-")
				.Append(p)
				.Append("-")
				.Append(q);
			return Obfuscator.Obfuscate(key.ToString());
		}

		public static RSAParameters Unpack(string rsap)
		{
			rsap = Obfuscator.DeObfuscate(rsap);
			RSAParameters key = new RSAParameters();
			string[] c = rsap.Split('-');
			key.D = !string.IsNullOrWhiteSpace(c[0]) ? c[0].FromHexString() : null;
			key.DP = !string.IsNullOrWhiteSpace(c[1]) ? c[1]?.FromHexString() : null;
			key.DQ = !string.IsNullOrWhiteSpace(c[2]) ? c[2]?.FromHexString() : null;
			key.Exponent = !string.IsNullOrWhiteSpace(c[3]) ? c[3]?.FromHexString() : null;
			key.InverseQ = !string.IsNullOrWhiteSpace(c[4]) ? c[4]?.FromHexString() : null;
			key.Modulus = !string.IsNullOrWhiteSpace(c[5]) ? c[5]?.FromHexString() : null;
			key.P = !string.IsNullOrWhiteSpace(c[6]) ? c[6]?.FromHexString() : null;
			key.Q = !string.IsNullOrWhiteSpace(c[7]) ? c[7]?.FromHexString() : null;
			return key;
		}

		public RsaKeyPair(RSAParameters prv, RSAParameters pbl)
		{
			RsaPrivate = prv;
			RsaPublic = pbl;

			Private = Pack(prv);
			Public = Pack(pbl);
		}

		public RsaKeyPair(string prv, string pbl)
		{
			Private = prv;
			Public = pbl;

			if (prv != null)
				RsaPrivate = Unpack(prv);
			if (pbl != null)
				RsaPublic = Unpack(pbl);
		}

		public RsaKeyPair(IAsymmetricCryptographerKeyPair key)
			: this(key.Private, key.Public)
		{ }

		public RsaKeyPair(RsaKeyPair key)
			: this(key.RsaPrivate, key.RsaPublic)
		{ }
	}

	public class RsaCryptographer : IAsymmetricCryptographer
	{
		// You can not change the KeySize:
		//   Xamarin.macOS (default 1024)
		//   Xamarin.iOS (default 1024)
		//   Xamarin.Android (default 1024)
		//   .NET 4.6.1 Windows 10 (default 1024)

		// You can change the KeySize:
		//   .NET Core 1.1 Windows 10 (default 2048)
		//   UWP -> (default 2048)

		static readonly byte[] _magic = { 0xe3, 0xd1, 0x11, 0x44 };
		static readonly byte _pkcs1 = 0x01;
		static readonly byte _key1024 = 0x01;
		static readonly int _signatureSize = 6;

		RSA _algImpl
		{
			get {
				RSA rsa = RSA.Create();
				rsa.KeySize = 1024;

				if (rsa.KeySize != 1024)
					throw new CryptographicException(T.Localized("RSA_UnsupportedKeySize"));

				return rsa;
			}
		}

		public IAsymmetricCryptographerKeyPair GenerateKeyPair()
		{
			using (RSA rsa = _algImpl)
			{
				RSAParameters p = rsa.ExportParameters(true);
				RSAParameters pp = rsa.ExportParameters(false);

				return new RsaKeyPair(p, pp);
			}
		}

		public ByteBuffer Encrypt(IByteBuffer msg, IAsymmetricCryptographerKeyPair pblKey)
		{
			if (pblKey == null || pblKey.Public == null)
				throw new ArgumentNullException(nameof(pblKey));
			if (pblKey.Algoritm != AsymmetricCryptographerAlgoritm.Rsa)
				throw new ArgumentException(T.Localized("RSA_BadKey"), nameof(pblKey));
			if (msg == null)
				throw new ArgumentNullException(nameof(msg));

			try
			{
				using (RSA rsa = _algImpl)
				{
					RsaKeyPair key = new RsaKeyPair(null, pblKey.Public);
					rsa.ImportParameters(key.RsaPublic);

					byte[] msgAsArray = msg.GetBytes();
					int keySize = rsa.KeySize / 8;
					int chunkSize = key.RsaPublic.Modulus.Length - 11;
					int numberOfChunks = msg.Length / chunkSize + 1;
					List<byte> emsg = new List<byte>(keySize * numberOfChunks);

					emsg.AddRange(_magic);
					emsg.Add(_pkcs1);
					emsg.Add(_key1024);

					for (int i = 0; i < numberOfChunks; i++)
					{
						int currentChunkSize = Math.Min(msgAsArray.Length - i * chunkSize, chunkSize);
						if (currentChunkSize > 0)
						{
							byte[] chunk = new byte[currentChunkSize];
							Array.Copy(msgAsArray, i * chunkSize, chunk, 0, currentChunkSize);

							byte[] echunk = rsa.Encrypt(chunk, RSAEncryptionPadding.Pkcs1);

							emsg.AddRange(echunk);
						}
					}

					return new ByteBuffer(emsg.ToArray());
				}
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				throw ex;
			}
			finally
			{
				msg.DisposeBytes();
			}
		}

		public ByteBuffer Decrypt(IByteBuffer msg, IAsymmetricCryptographerKeyPair prvKey)
		{
			if (prvKey == null || prvKey.Private == null)
				throw new ArgumentNullException(nameof(prvKey));
			if (prvKey.Algoritm != AsymmetricCryptographerAlgoritm.Rsa)
				throw new ArgumentException(T.Localized("RSA_BadKey"), nameof(prvKey));
			if (msg == null)
				throw new ArgumentNullException(nameof(msg));

			try
			{
				byte[] amsg = msg.GetBytes();
				if (!amsg.Take(4).SequenceEqual(_magic) || amsg[4] != _pkcs1 || amsg[5] != _key1024)
					throw new ArgumentException(T.Localized("RSA_IncrorrectFormat"), nameof(msg));

				using (RSA rsa = _algImpl)
				{
					RsaKeyPair key = new RsaKeyPair(prvKey.Private, null);
					rsa.ImportParameters(key.RsaPrivate);

					int keySize = rsa.KeySize / 8;
					int chunkSize = keySize;
					int numberOfChunks = msg.Length / chunkSize + 1;
					List<byte> dmsg = new List<byte>(keySize * numberOfChunks);

					for (int i = 0; i < numberOfChunks; i++)
					{
						int currentChunkSize = Math.Min((amsg.Length - _signatureSize) - i * chunkSize, chunkSize);
						if (currentChunkSize > 0)
						{
							byte[] chunk = new byte[currentChunkSize];
							Array.Copy(amsg, _signatureSize + i * chunkSize, chunk, 0, currentChunkSize);

							byte[] dchunk = rsa.Decrypt(chunk, RSAEncryptionPadding.Pkcs1);

							dmsg.AddRange(dchunk);
						}
					}

					return new ByteBuffer(dmsg.ToArray());
				}
			}
			catch (ArgumentException ex)
			{
				throw ex;
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				throw ex;
			}
			finally
			{
				msg.DisposeBytes();
			}
		}

		public ByteBuffer Sign(IByteBuffer msg, IAsymmetricCryptographerKeyPair prvKey)
		{
			if (prvKey == null || prvKey.Private == null)
				throw new ArgumentNullException(nameof(prvKey));
			if (prvKey.Algoritm != AsymmetricCryptographerAlgoritm.Rsa)
				throw new ArgumentException(T.Localized("RSA_BadKey"), nameof(prvKey));
			if (msg == null)
				throw new ArgumentNullException(nameof(msg));

			try
			{
				using (RSA rsa = _algImpl)
				{
					RsaKeyPair key = new RsaKeyPair(prvKey.Private, null);
					rsa.ImportParameters(key.RsaPrivate);
					return new ByteBuffer(rsa.SignData(msg.GetBytes(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
				}
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				throw ex;
			}
			finally
			{
				msg.DisposeBytes();
			}
		}

		public bool Verify(IByteBuffer msg, IByteBuffer signature, IAsymmetricCryptographerKeyPair pblKey)
		{
			if (pblKey == null || pblKey.Public == null)
				throw new ArgumentNullException(nameof(pblKey));
			if (pblKey.Algoritm != AsymmetricCryptographerAlgoritm.Rsa)
				throw new ArgumentException(T.Localized("RSA_BadKey"), nameof(pblKey));
			if (msg == null)
				throw new ArgumentNullException(nameof(msg));
			if (signature == null)
				throw new ArgumentNullException(nameof(signature));

			try
			{
				using (RSA rsa = _algImpl)
				{
					RsaKeyPair key = new RsaKeyPair(null, pblKey.Public);
					rsa.ImportParameters(key.RsaPublic);
					return rsa.VerifyData(msg.GetBytes(), signature.GetBytes(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
				}
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				throw ex;
			}
			finally
			{
				msg.DisposeBytes();
				signature.DisposeBytes();
			}
		}

		public void Dispose()
		{
		}
	}
}
