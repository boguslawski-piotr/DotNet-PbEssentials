#if WINDOWS_UWP

using System;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace pbXNet
{
	public partial class AesCryptographer : ICryptographer
	{
		public IByteBuffer GenerateKey(IPassword pwd, IByteBuffer salt)
		{
			const int iterations = 10000;

			KeyDerivationAlgorithmProvider objKdfProv = KeyDerivationAlgorithmProvider.OpenAlgorithm(KeyDerivationAlgorithmNames.Pbkdf2Sha1);

			IBuffer buffPwd = CryptographicBuffer.CreateFromByteArray(pwd.GetBytes());
			pwd.Dispose();
			IBuffer buffSalt = CryptographicBuffer.CreateFromByteArray(salt.GetBytes());
			salt.DisposeBytes();

			KeyDerivationParameters pbkdf2Params = KeyDerivationParameters.BuildForPbkdf2(buffSalt, iterations);
			CryptographicKey keyOriginal = objKdfProv.CreateKey(buffPwd);
			IBuffer buffKey = CryptographicEngine.DeriveKeyMaterial(keyOriginal, pbkdf2Params, 32);

			return new SecureBuffer(IBufferToByteArray(buffKey));
		}

		public IByteBuffer GenerateIV()
		{
			IBuffer buff = CryptographicBuffer.GenerateRandom(16);
			return new SecureBuffer(IBufferToByteArray(buff));
		}

		public ByteBuffer Encrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv)
		{
			return Transform(msg, key, iv, true);
		}

		public ByteBuffer Decrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv)
		{
			return Transform(msg, key, iv, false);
		}

		ByteBuffer Transform(IByteBuffer msg, IByteBuffer key, IByteBuffer iv, bool encrypt)
		{
			try
			{
				// algoritm
				SymmetricKeyAlgorithmProvider objAlg = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);

				// prepare
				IBuffer buffKey = CryptographicBuffer.CreateFromByteArray(key.GetBytes());
				key.DisposeBytes();
				CryptographicKey ckey = objAlg.CreateSymmetricKey(buffKey);

				IBuffer buffMsg = CryptographicBuffer.CreateFromByteArray(msg.GetBytes());
				msg.DisposeBytes();

				IBuffer buffIv = CryptographicBuffer.CreateFromByteArray(iv.GetBytes());
				iv.DisposeBytes();

				// encrypt/decrypt
				IBuffer buffMsgTransformed = encrypt ? CryptographicEngine.Encrypt(ckey, buffMsg, buffIv) : CryptographicEngine.Decrypt(ckey, buffMsg, buffIv);
				return new ByteBuffer(IBufferToByteArray(buffMsgTransformed));
			}
			catch (Exception e)
			{
				Log.E(e.Message, this);
				return new ByteBuffer();
			}
		}

		private static byte[] IBufferToByteArray(IBuffer buf)
		{
			byte[] rawBytes = new byte[buf.Length];
			using (var reader = DataReader.FromBuffer(buf))
				reader.ReadBytes(rawBytes);
			return rawBytes;
		}

		public void Dispose()
		{
		}
	}
}

#endif
