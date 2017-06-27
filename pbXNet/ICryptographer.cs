using System;

namespace pbXNet
{
	public interface ICryptographer : IDisposable
	{
		IByteBuffer GenerateKey(IPassword pwd, IByteBuffer salt);
		
		/// The function should never return null. 
		/// If IV is unnecessary in the implemented algorithm, 
		/// the function should return an empty ByteBuffer.
		IByteBuffer GenerateIV();

		/// When error during encryption should return an empty ByteBuffer.
		/// Other errors, such as incorrect parameters, should be handled with exceptions.
		ByteBuffer Encrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv);

		/// When error during decryption should return an empty ByteBuffer.
		/// Other errors, such as incorrect parameters, should be handled with exceptions.
		ByteBuffer Decrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv);
	}
}
