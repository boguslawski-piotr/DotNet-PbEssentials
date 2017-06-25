using System;
using System.Security.Cryptography;
using System.Text;

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
			string s = key.ToString();
			return Obfuscator.Obfuscate(s);
		}

		public static RSAParameters Unpack(string rsap)
		{
			rsap = Obfuscator.DeObfuscate(rsap);
			RSAParameters key = new RSAParameters();
			string[] components = rsap.Split('-');
			key.D = !string.IsNullOrWhiteSpace(components[0]) ? components[0].FromHexString() : null;
			key.DP = !string.IsNullOrWhiteSpace(components[1]) ? components[1]?.FromHexString() : null;
			key.DQ = !string.IsNullOrWhiteSpace(components[2]) ? components[2]?.FromHexString() : null;
			key.Exponent = !string.IsNullOrWhiteSpace(components[3]) ? components[3]?.FromHexString() : null;
			key.InverseQ = !string.IsNullOrWhiteSpace(components[4]) ? components[4]?.FromHexString() : null;
			key.Modulus = !string.IsNullOrWhiteSpace(components[5]) ? components[5]?.FromHexString() : null;
			key.P = !string.IsNullOrWhiteSpace(components[6]) ? components[6]?.FromHexString() : null;
			key.Q = !string.IsNullOrWhiteSpace(components[7]) ? components[7]?.FromHexString() : null;
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
		RSA _algImpl
		{
			get {
				RSA rsa = RSA.Create();
				rsa.KeySize = 2048;
				return rsa;
			}
		}

		public IAsymmetricCryptographerKeyPair GenerateKeyPair(int length = -1)
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
			if (pblKey.Algoritm != AsymmetricCryptographerAlgoritm.Rsa)
				throw new ArgumentException("Key does not fit this algorithm.", nameof(pblKey));
			if (pblKey.Public == null)
				throw new ArgumentNullException(nameof(pblKey.Public));

			try
			{
				using (RSA rsa = _algImpl)
				{
					RsaKeyPair key = new RsaKeyPair(null, pblKey.Public);
					rsa.ImportParameters(key.RsaPublic);

					KeySizes[] keySizes = rsa.LegalKeySizes;
					if (keySizes?.Length < 1)
						throw new MissingFieldException($"There is no data in the {nameof(rsa.LegalKeySizes)} field.");

					byte[] msgAsArray = msg.GetBytes();
					int chunkSize = rsa.KeySize / 8 - keySizes[0].SkipSize;
					int numberOfChunks = msg.Length / chunkSize + 1;
					byte[] emsg = new byte[0];

					for (int i = 0; i < numberOfChunks; i++)
					{
						int currentChunkSize = Math.Min(msgAsArray.Length - i * chunkSize, chunkSize);
						if (currentChunkSize > 0)
						{
							byte[] chunk = new byte[currentChunkSize];
							Array.Copy(msgAsArray, i * chunkSize, chunk, 0, currentChunkSize);

							byte[] echunk = rsa.Encrypt(chunk, RSAEncryptionPadding.Pkcs1);

							int emsgLength = emsg.Length;
							Array.Resize(ref emsg, emsgLength + echunk.Length);
							Array.Copy(echunk, 0, emsg, emsgLength, echunk.Length);
						}
					}

					return new ByteBuffer(emsg);
				}
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				return new ByteBuffer();
			}
			finally
			{
				msg.DisposeBytes();
			}
		}

		public ByteBuffer Decrypt(IByteBuffer msg, IAsymmetricCryptographerKeyPair prvKey)
		{
			if (prvKey.Algoritm != AsymmetricCryptographerAlgoritm.Rsa)
				throw new ArgumentException("Key does not fit this algorithm.", nameof(prvKey));
			if (prvKey.Private == null)
				throw new ArgumentNullException(nameof(prvKey.Private));

			try
			{
				using (RSA rsa = _algImpl)
				{
					RsaKeyPair key = new RsaKeyPair(prvKey.Private, null);
					rsa.ImportParameters(key.RsaPrivate);

					byte[] msgAsArray = msg.GetBytes();
					int chunkSize = rsa.KeySize / 8;
					int numberOfChunks = msg.Length / chunkSize + 1;
					byte[] dmsg = new byte[0];

					for (int i = 0; i < numberOfChunks; i++)
					{
						int currentChunkSize = Math.Min(msgAsArray.Length - i * chunkSize, chunkSize);
						if (currentChunkSize > 0)
						{
							byte[] chunk = new byte[currentChunkSize];
							Array.Copy(msgAsArray, i * chunkSize, chunk, 0, currentChunkSize);

							byte[] dchunk = rsa.Decrypt(chunk, RSAEncryptionPadding.Pkcs1);

							int dmsgLength = dmsg.Length;
							Array.Resize(ref dmsg, dmsgLength + dchunk.Length);
							Array.Copy(dchunk, 0, dmsg, dmsgLength, dchunk.Length);
						}
					}

					return new ByteBuffer(dmsg);
				}
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				return new ByteBuffer();
			}
			finally
			{
				msg.DisposeBytes();
			}
		}

		public ByteBuffer Sign(IByteBuffer msg, IAsymmetricCryptographerKeyPair prvKey)
		{
			if (prvKey.Algoritm != AsymmetricCryptographerAlgoritm.Rsa)
				throw new ArgumentException("Key does not fit this algorithm.", nameof(prvKey));
			if (prvKey.Private == null)
				throw new ArgumentNullException(nameof(prvKey.Private));

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
				return new ByteBuffer();
			}
			finally
			{
				msg.DisposeBytes();
			}
		}

		public bool Verify(IByteBuffer msg, IByteBuffer signature, IAsymmetricCryptographerKeyPair pblKey)
		{
			if (pblKey.Algoritm != AsymmetricCryptographerAlgoritm.Rsa)
				throw new ArgumentException("Key does not fit this algorithm.", nameof(pblKey));
			if (pblKey.Public == null)
				throw new ArgumentNullException(nameof(pblKey.Public));

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
				return false;
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
