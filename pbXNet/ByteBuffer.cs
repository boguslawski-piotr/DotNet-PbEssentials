using System;
using System.IO;
using System.Text;

namespace pbXNet
{
	public class ByteBuffer : IByteBuffer, IDisposable
	{
		byte[] _b;

		public ByteBuffer(byte[] b, bool clearSource = false)
		{
			_b = (byte[])b.Clone();

			if (clearSource && !b.IsReadOnly)
			{
				b.FillWith<byte>(0);
				Array.Resize<byte>(ref b, 0);
			}
		}

		public ByteBuffer(char[] cb, bool clearSource = false)
			: this(Encoding.UTF8.GetBytes(cb), true)
		{
			if (clearSource && !cb.IsReadOnly)
			{
				cb.FillWith<char>('\0');
				Array.Resize<char>(ref cb, 0);
			}
		}

		public ByteBuffer(string sb)
			: this(Encoding.UTF8.GetBytes(sb), true)
		{
		}

		public uint Length => (uint)(_b == null ? 0 : _b.Length);

		public byte[] GetBytes()
		{
			return _b;
		}

		public static implicit operator byte[] (ByteBuffer bb)
		{
			return bb.GetBytes();
		}

		public static implicit operator char[] (ByteBuffer bb)
		{
			return Encoding.UTF8.GetChars(bb);
		}

		public static implicit operator string(ByteBuffer bb)
		{
			return Encoding.UTF8.GetString(bb._b);
		}

		public override string ToString()
		{
			return _b?.ToHexString();
		}

		public void DisposeBytes()
		{
		}

		public void Dispose()
		{
			_b?.FillWith<byte>(0);
			_b = null;
		}
	}
}