using System;

namespace pbXNet
{
	public interface ICryptographer : IDisposable
	{
		IByteBuffer GenerateKey(IPassword pwd, IByteBuffer salt, int length = 32);

		IByteBuffer GenerateIV(int length = 16);

		/// When error should return empty ByteBuffer.
		ByteBuffer Encrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv);

		/// When error should return empty ByteBuffer.
		ByteBuffer Decrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv);
	}
}
