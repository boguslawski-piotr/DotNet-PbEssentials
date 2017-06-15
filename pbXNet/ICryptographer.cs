using System;

namespace pbXNet
{
	public interface ICryptographer : IDisposable
	{
		byte[] GenerateKey(IPassword pwd, byte[] salt, int length = 32);

		byte[] GenerateIV(int length = 16);

		byte[] Encrypt(byte[] msg, byte[] key, byte[] iv);

		byte[] Decrypt(byte[] msg, byte[] key, byte[] iv);
	}
}
