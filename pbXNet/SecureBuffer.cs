using System;
using System.IO;
using System.Text;

namespace pbXNet
{
	public class SecureBuffer : IByteBuffer, IDisposable
	{
		uint _l;
		byte[] _b;
		byte[] _unsecureb;

		public SecureBuffer(byte[] b, bool clearSource = false)
		{
			_l = (uint)(b == null ? 0 : b.Length);
			using (MemoryStream sb = new MemoryStream(b))
			{
				using (MemoryStream csb = new DeflateCompressor().Compress<MemoryStream>(sb))
				{
					csb.Position = 0;
					_b = csb.ToArray();
					csb.GetBuffer().FillWith<byte>(0);
				}
			}

			if (clearSource && !b.IsReadOnly)
			{
				b.FillWith<byte>(0);
				Array.Resize<byte>(ref b, 0);
			}
		}

		public SecureBuffer(char[] cb, bool clearSource = false)
			: this(Encoding.UTF8.GetBytes(cb), true)
		{
			if (clearSource && !cb.IsReadOnly)
			{
				cb.FillWith<char>('\0');
				Array.Resize<char>(ref cb, 0);
			}
		}

		public SecureBuffer(string sb)
			: this(Encoding.UTF8.GetBytes(sb), true)
		{
		}

		public uint Length => _l;

		public byte[] GetBytes()
		{
			if (_unsecureb != null)
				return _unsecureb;

			using (MemoryStream csb = new MemoryStream(_b))
			{
				using (MemoryStream sb = new DeflateCompressor().Decompress<MemoryStream>(csb))
				{
					sb.Position = 0;
					_unsecureb = sb.ToArray();
					sb.GetBuffer().FillWith<byte>(0);
				}
			}

			return _unsecureb;
		}

		public void DisposeBytes()
		{
			_unsecureb?.FillWith<byte>(0);
			_unsecureb = null;
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