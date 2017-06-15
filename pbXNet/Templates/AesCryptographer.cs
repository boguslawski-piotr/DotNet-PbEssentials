using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace pbXNet
{
	public partial class AesCryptographer : ICryptographer
	{
		public byte[] GenerateKey(Password pwd, byte[] salt, int length = 32)
		{
			throw new NotImplementedException();
		}

		public byte[] GenerateIV(int length = 16)
		{
			throw new NotImplementedException();
		}

		public byte[] Encrypt(byte[] msg, byte[] key, byte[] iv)
		{
			throw new NotImplementedException();
		}

		public byte[] Decrypt(byte[] msg, byte[] key, byte[] iv)
		{
			throw new NotImplementedException();
		}
	}
}
