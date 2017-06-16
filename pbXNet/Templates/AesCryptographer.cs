using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace pbXNet
{
	public partial class AesCryptographer : ICryptographer
	{
		public IByteBuffer GenerateKey(Password pwd, IByteBuffer salt, int length = 32)
		{
			throw new NotImplementedException();
		}

		public IByteBuffer GenerateIV(int length = 16)
		{
			throw new NotImplementedException();
		}

		public ByteBuffer Encrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv)
		{
			throw new NotImplementedException();
		}

		public ByteBuffer Decrypt(IByteBuffer msg, IByteBuffer key, IByteBuffer iv)
		{
			throw new NotImplementedException();
		}
	}
}
