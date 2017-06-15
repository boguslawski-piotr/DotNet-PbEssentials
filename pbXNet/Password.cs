using System;
using System.Text;
using System.Linq;

namespace pbXNet
{
	public class Password : IPassword, IDisposable
	{
		char[] _passwd;
		byte[] _bpasswd;

		public int Length => _passwd.Length;

		public Password()
		{
			_passwd = new char[] { };
		}

		public Password(IPassword p)
		{
			_passwd = Encoding.UTF8.GetChars(p.GetBytes());
			p.DisposeBytes();
		}

		public Password(Password p)
		{
			_passwd = (char[])p._passwd.Clone();
		}

		public Password(string passwd)
		{
			_passwd = passwd.ToCharArray();
		}

		public void Dispose()
		{
			DisposeBytes();
			_passwd?.FillWith<char>('\0');
			_passwd = new char[] { };
		}

		public byte[] GetBytes()
		{
			DisposeBytes();
			_bpasswd = Encoding.UTF8.GetBytes(_passwd);
			return _bpasswd;
		}

		public void DisposeBytes()
		{
			_bpasswd?.FillWith<byte>(0);
			_bpasswd = null;
		}

		public void Append(char c)
		{
			DisposeBytes();
			Array.Resize<char>(ref _passwd, _passwd.Length + 1);
			_passwd[_passwd.Length - 1] = c;
		}

		public void RemoveLast()
		{
			DisposeBytes();
			Array.Resize<char>(ref _passwd, _passwd.Length - 1);
		}

		public static implicit operator byte[] (Password p)
		{
			return p.GetBytes();
		}

		public static Password operator +(Password d, char c)
		{
			d.Append(c);
			return d;
		}

		public override bool Equals(object obj)
		{
			Password p = obj as Password;
			if (p == null)
				return false;
			return this.Equals(p);
		}

		public bool Equals(Password p)
		{
			if (object.ReferenceEquals(p, null))
				return false;
			if (object.ReferenceEquals(this, p))
				return true;
			if (this.GetType() != p.GetType())
				return false;

			return _passwd.SequenceEqual(p._passwd);
		}

		public static bool operator ==(Password l, Password r)
		{
			if (object.ReferenceEquals(l, null))
			{
				if (object.ReferenceEquals(r, null))
					return true;
				return false;
			}
			return l.Equals(r);
		}

		public static bool operator !=(Password l, Password r)
		{
			return !(l == r);
		}

		public override int GetHashCode()
		{
			return _passwd.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format($"[Length={Length}] {_passwd?.ToHexString()}");
		}
	}
}