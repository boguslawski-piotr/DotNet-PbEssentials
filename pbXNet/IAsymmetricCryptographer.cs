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

		/// When error during (!) encryption should return an empty ByteBuffer.
		/// Other errors, such as incorrect parameters, should be handled with exceptions.
		ByteBuffer Encrypt(IByteBuffer msg, IAsymmetricCryptographerKeyPair pblKey);

		/// When error during decryption should return an empty ByteBuffer.
		/// Other errors, such as incorrect parameters, should be handled with exceptions.
		ByteBuffer Decrypt(IByteBuffer msg, IAsymmetricCryptographerKeyPair prvKey);

		/// When error during signing should return an empty ByteBuffer.
		/// Other errors, such as incorrect parameters, should be handled with exceptions.
		ByteBuffer Sign(IByteBuffer msg, IAsymmetricCryptographerKeyPair prvKey);

		bool Verify(IByteBuffer msg, IByteBuffer signature, IAsymmetricCryptographerKeyPair pblKey);
	}
}
