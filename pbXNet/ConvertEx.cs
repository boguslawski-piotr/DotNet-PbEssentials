using System.Globalization;
using System.IO;
using System.Text;

namespace pbXNet
{
	public static class ConvertEx
	{
		public static string ToHexString(byte[] d)
		{
			string s = "";
			foreach (byte b in d)
				s += $"{b:X2}";
			return s;
		}

		public static string ToHexString(char[] d)
		{
			return ToHexString(Encoding.UTF8.GetBytes(d));
		}

		public static string ToHexString(MemoryStream src)
		{
			return ToHexString(src.ToArray());
		}

		public static byte[] FromHexString(string d)
		{
			byte[] da = new byte[d.Length / 2];
			for (int i = 0; i < d.Length / 2; i++)
			{
				da[i] = byte.Parse(d.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
			}
			return da;
		}

		public static string ToString(MemoryStream src)
		{
			return Encoding.UTF8.GetString(src.ToArray(), 0, (int)src.Length);
		}

		public static byte[] ToByteArray(string d)
		{
			return Encoding.UTF8.GetBytes(d);
		}

		public static MemoryStream ToMemoryStream(string d)
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(d));
		}
	}
}
