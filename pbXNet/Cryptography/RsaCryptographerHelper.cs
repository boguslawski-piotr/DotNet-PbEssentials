using System;
using System.Text;

namespace pbXNet
{
	public static class RsaCryptographerHelper
	{
		static Lazy<RsaCryptographer> _cryptographer = new Lazy<RsaCryptographer>(() => new RsaCryptographer());
		public static RsaCryptographer Cryptographer => _cryptographer.Value;

		public static string Sign(string data, IAsymmetricCryptographerKeyPair prvKey)
		{
			Check.Null(data, nameof(data));

			ByteBuffer bdata = new ByteBuffer(data, Encoding.UTF8);
			ByteBuffer signature = Cryptographer.Sign(bdata, prvKey);
			bdata.DisposeBytes();
			return signature.ToHexString();
		}

		public static bool Verify(string data, string signature, IAsymmetricCryptographerKeyPair pblKey)
		{
			Check.Null(data, nameof(data));
			Check.Empty(signature, nameof(signature));

			ByteBuffer bdata = new ByteBuffer(data, Encoding.UTF8);
			ByteBuffer bsignature = ByteBuffer.NewFromHexString(signature);
			bool ok = Cryptographer.Verify(bdata, bsignature, pblKey);
			bdata.DisposeBytes();
			bsignature.DisposeBytes();
			return ok;
		}

		public static string Encrypt(string data, IAsymmetricCryptographerKeyPair pblKey)
		{
			Check.Null(data, nameof(data));

			ByteBuffer bdata = new ByteBuffer(data, Encoding.UTF8);
			ByteBuffer edata = Cryptographer.Encrypt(bdata, pblKey);
			bdata.DisposeBytes();
			return edata.ToHexString();
		}

		public static string Decrypt(string data, IAsymmetricCryptographerKeyPair prvKey)
		{
			Check.Null(data, nameof(data));

			ByteBuffer bdata = ByteBuffer.NewFromHexString(data);
			ByteBuffer ddata = Cryptographer.Decrypt(bdata, prvKey);
			bdata.DisposeBytes();
			return ddata.ToString(Encoding.UTF8);
		}
	}
}
