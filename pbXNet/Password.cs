using System;
using System.Text;
using System.Linq;

namespace pbXNet
{
	public class Password : IDisposable
	{
		char[] _passwd;
		byte[] _bpasswd;

		public int Length => _passwd.Length;

		public Password()
		{
			_passwd = new char[] { };
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
			Clear(true);
		}

		public void Clear(bool allData)
		{
			_bpasswd?.FillWith<byte>(0);
			_bpasswd = null;

			if (allData)
			{
				_passwd?.FillWith<char>('\0');
				_passwd = new char[] { };
			}
		}

		public void Append(char c)
		{
			Clear(false);
			Array.Resize<char>(ref _passwd, _passwd.Length + 1);
			_passwd[_passwd.Length - 1] = c;
		}

		public void RemoveLast()
		{
			Clear(false);
			Array.Resize<char>(ref _passwd, _passwd.Length - 1);
		}

		public static implicit operator byte[] (Password p)
		{
			p.Clear(false);
			p._bpasswd = Encoding.UTF8.GetBytes(p._passwd);
			return p._bpasswd;
		}

		public static Password operator +(Password d, char c)
		{
			d.Append(c);
			return d;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as Password);
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