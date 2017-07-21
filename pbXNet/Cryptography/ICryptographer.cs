using System;

namespace pbXNet
{
	public interface ICryptographer
	{
		IByteBuffer GenerateKey(IPassword pwd, IByteBuffer salt);
		
		/// The function should never return null. If IV is unnecessary in 
		/// the implemented algorithm, the function should return an empty ByteBuffer.
		IByteBuffer GenerateIV();

		ByteBuffer Encrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv);

		ByteBuffer Decrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv);
	}
}
