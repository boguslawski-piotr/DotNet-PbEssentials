using System;
using System.IO;
using System.Threading.Tasks;

namespace pbXNet
{
	public partial class AesCryptographer : ICryptographer
	{
		public async Task<byte[]> EncryptAsync(byte[] msg, byte[] key, byte[] iv)
		=> await Task.Run(() => Encrypt(msg, key, iv));

		public async Task<byte[]> DecryptAsync(byte[] msg, byte[] key, byte[] iv)
			=> await Task.Run(() => Decrypt(msg, key, iv));

		// You will find the rest of implementation in the platform directories...
	}
}
