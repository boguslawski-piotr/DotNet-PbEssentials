using System;

namespace pbXNet
{
	public enum AsymmetricCryptographerAlgoritm
	{
		Rsa,
	}

	public interface IAsymmetricCryptographerKeyPair
	{
		AsymmetricCryptographerAlgoritm Algoritm { get; }

		string Public { get; }
		string Private { get; }
	};

	public interface IAsymmetricCryptographer : IDisposable
	{
		IAsymmetricCryptographerKeyPair GenerateKeyPair();

		ByteBuffer Encrypt(IByteBuffer msg, IAsymmetricCryptographerKeyPair pblKey);

		ByteBuffer Decrypt(IByteBuffer msg, IAsymmetricCryptographerKeyPair prvKey);

		ByteBuffer Sign(IByteBuffer msg, IAsymmetricCryptographerKeyPair prvKey);

		bool Verify(IByteBuffer msg, IByteBuffer signature, IAsymmetricCryptographerKeyPair pblKey);
	}
}
