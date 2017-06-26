using System;

namespace pbXNet
{
	public interface ICryptographer : IDisposable
	{
		IByteBuffer GenerateKey(IPassword pwd, IByteBuffer salt, int length = 32);
		
		/// The function should never return null. 
		/// If IV is unnecessary in the implemented algorithm, 
		/// the function should return an empty ByteBuffer.
		IByteBuffer GenerateIV(int length = 16);

		/// When error should return an empty ByteBuffer.
		ByteBuffer Encrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv);

		/// When error should return an empty ByteBuffer.
		ByteBuffer Decrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv);
	}
}
