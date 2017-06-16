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

		public Password(Password p)
		{
			_passwd = (char[])p._passwd.Clone();
		}

		public Password(string passwd)
		{
			_passwd = passwd.ToCharArray();
			Obfuscate();
		}

		public byte[] GetBytes()
		{
			DisposeBytes();
			_bpasswd = Encoding.UTF8.GetBytes(_passwd);
			return _bpasswd;
		}

		public void Append(char c)
		{
			DisposeBytes();
			System.Array.Resize<char>(ref _passwd, _passwd.Length + 1);
			Obfuscate(ref c);
			_passwd[_passwd.Length - 1] = c;
		}

		public void RemoveLast()
		{
			DisposeBytes();
			System.Array.Resize<char>(ref _passwd, _passwd.Length - 1);
		}

		public static Password operator +(Password d, char c)
		{
			d.Append(c);
			return d;
		}

		void Obfuscate(ref char c)
		{
			int i = (int)c;
			i = ~i;
			c = (char)i;
		}

		void Obfuscate()
		{
			for (int i = 0; i < Length; i++)
				Obfuscate(ref _passwd[i]);
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
			if (_passwd == null)
				return p == null;
			
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
			return _passwd?.ToHexString();
		}

		public void DisposeBytes()
		{
			_bpasswd?.FillWith<byte>(0);
			_bpasswd = null;
		}

		public void Dispose()
		{
			DisposeBytes();
			_passwd?.FillWith<char>('\0');
			_passwd = new char[] { };
		}
	}
}