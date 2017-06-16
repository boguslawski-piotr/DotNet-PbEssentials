using System;

namespace pbXNet
{
	public interface IAsymmetricCryptographer : IDisposable
	{
		(IByteBuffer pbl, IByteBuffer prv) GenerateKeyPair(int length = -1);

		ByteBuffer Encrypt(IByteBuffer msg, IByteBuffer pblKey);

		ByteBuffer Decrypt(IByteBuffer msg, IByteBuffer prvKey);

		ByteBuffer Sign(IByteBuffer msg, IByteBuffer prvKey);

		bool Verify(IByteBuffer msg, IByteBuffer signature, IByteBuffer pblKey);
	}
}
