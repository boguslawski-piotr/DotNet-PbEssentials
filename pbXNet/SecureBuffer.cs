using System;
using System.IO;

namespace pbXNet
{
	public class SecureBuffer : IByteBuffer, IDisposable
	{
		int _l;
		byte[] _b;
		byte[] _nsb;

		public int Length => _l;

		public SecureBuffer()
		{
		}

		public SecureBuffer(byte[] b, bool clearSource = false)
		{
			Build(b);

			if (clearSource && !b.IsReadOnly)
			{
				b.FillWith<byte>(0);
				System.Array.Resize<byte>(ref b, 0);
			}
		}

		public SecureBuffer(IByteBuffer b, bool clearSource = false)
		{
			Build(b.GetBytes());

			if (!clearSource)
				b.DisposeBytes();
			else
				b.Dispose();
		}

		public SecureBuffer(SecureBuffer b, bool clearSource = false)
		{
			_l = b._l;
			_b = (byte[])b._b.Clone();

			if (clearSource)
				b.Dispose();
		}

		void Build(byte[] b)
		{
			Dispose();
			if (b == null)
				return;

			_l = b.Length;

			using (MemoryStream sb = new MemoryStream(b))
			{
				using (MemoryStream csb = new DeflateCompressor().Compress<MemoryStream>(sb))
				{
					csb.Position = 0;
					_b = csb.ToArray();
					csb.GetBuffer().FillWith<byte>(0);
				}
			}
		}

		public SecureBuffer FromHexString(string d)
		{
			Dispose();
			_b = d?.FromHexString();
			if (_b != null)
			{
				_l = GetBytes().Length;
				DisposeBytes();
			}
			return this;
		}

		public static SecureBuffer NewFromHexString(string d)
		{
			return new SecureBuffer().FromHexString(d);
		}

		public byte[] GetBytes()
		{
			if (_nsb != null)
				return _nsb;
			if (_b == null)
				return null;

			using (MemoryStream csb = new MemoryStream(_b))
			{
				using (MemoryStream sb = new DeflateCompressor().Decompress<MemoryStream>(csb))
				{
					sb.Position = 0;
					_nsb = sb.ToArray();
					sb.GetBuffer().FillWith<byte>(0);
				}
			}

			return _nsb;
		}

		public string ToHexString()
		{
			return _b?.ToHexString();
		}

		public override string ToString()
		{
			return ToHexString();
		}

		public void DisposeBytes()
		{
			_nsb?.FillWith<byte>(0);
			_nsb = null;
		}

		public void Dispose()
		{
			DisposeBytes();
			_b?.FillWith<byte>(0);
			_b = null;
			_l = 0;
		}
	}
}