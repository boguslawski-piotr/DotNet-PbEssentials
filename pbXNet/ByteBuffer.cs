using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;

namespace pbXNet
{
	public class ByteBuffer : IByteBuffer, IDisposable, IEnumerable<byte>
	{
		readonly byte[] _b;

		public int Length => (_b == null ? 0 : _b.Length);

		public ByteBuffer()
		{
			_b = new byte[0];
		}

		public ByteBuffer(byte[] b, bool clearSource = false)
		{
			_b = (byte[])b.Clone();

			if (clearSource
#if !WINDOWS_UWP && !NETSTANDARD1_6
				&& !b.IsReadOnly
#endif
				)
			{
				b.FillWith<byte>(0);
				System.Array.Resize<byte>(ref b, 0);
			}
		}

		public ByteBuffer(IByteBuffer b, bool clearSource = false)
		{
			_b = (byte[])b.GetBytes().Clone();
			if (!clearSource)
				b.DisposeBytes();
			else
				b.Dispose();
		}

		public ByteBuffer(ByteBuffer b, bool clearSource = false)
		{
			_b = (byte[])b._b.Clone();
			if (clearSource)
				b.Dispose();
		}

		public ByteBuffer(string sb, Encoding encoding)
			: this(encoding.GetBytes(sb), true)
		{
		}

		public ByteBuffer(IEnumerable<byte> l)
			: this(l.ToArray(), true)
		{
		}

		public ByteBuffer(Stream s)
		{
			if (s is MemoryStream)
			{
				_b = (s as MemoryStream).ToArray();
			}
			else
			{
				using (MemoryStream sm = new MemoryStream())
				{
					if (s.CanSeek)
						s.Seek(0, SeekOrigin.Begin);
					s.CopyTo(sm);
					_b = sm.ToArray();
				}
			}
		}

		public static ByteBuffer NewFromHexString(string d)
		{
			return new ByteBuffer(d.FromHexString());
		}

		public static ByteBuffer NewFromString(string d, Encoding encoding)
		{
			return new ByteBuffer(encoding.GetBytes(d));
		}

		public virtual byte[] GetBytes()
		{
			return _b;
		}

		public static implicit operator byte[] (ByteBuffer bb)
		{
			return bb?.GetBytes();
		}

		public virtual string ToHexString()
		{
			return _b?.ToHexString();
		}

		public virtual string ToString(Encoding encoding)
		{
			return encoding.GetString(_b);
		}

		public override string ToString()
		{
			return ToHexString();
		}

		public override bool Equals(object obj)
		{
			ByteBuffer p = obj as ByteBuffer;
			if (p == null)
				return false;
			return this.Equals(p);
		}

		public bool Equals(ByteBuffer b)
		{
			if (object.ReferenceEquals(b, null))
				return false;
			if (object.ReferenceEquals(this, b))
				return true;
			if (this.GetType() != b.GetType())
				return false;

			if (_b == null || b._b == null)
				return _b == null && b._b == null;
			
			return _b.SequenceEqual(b._b);
		}

		public override int GetHashCode()
		{
			return _b.GetHashCode();
		}

		public IEnumerator<byte> GetEnumerator()
		{
			return new ArrayExtensions.Enumerator<byte>(_b);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _b.GetEnumerator();
		}

		public virtual void DisposeBytes()
		{
		}

		public virtual void Dispose()
		{
			_b?.FillWith<byte>(0);
		}
	}
}