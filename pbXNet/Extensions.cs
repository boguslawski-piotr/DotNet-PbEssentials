using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace pbXNet
{
	/// <summary>
	/// Arrays extensions.
	/// </summary>
	public static class ArrayExtensions
	{
		public static void FillWithDefault<VT>(this VT[] src) where VT: struct
		{
			for (int n = 0; n < src.Length; n++)
				src[n] = new VT();
		}

		public static void FillWithNull<NT>(this NT[] src) where NT: class
		{
			for (int n = 0; n < src.Length; n++)
				src[n] = null;
		}

		public static void FillWith<T>(this T[] src, T v)
		{
			for (int n = 0; n < src.Length; n++)
				src[n] = v;
		}

		public static string ToHexString(this byte[] src)
		{
			string d = ConvertEx.ToHexString(src);
			return d;
		}

		public static string ToHexString(this char[] src)
		{
			string d = ConvertEx.ToHexString(src);
			return d;
		}

		public class Enumerator<T> : IEnumerator<T>
		{
			T[] _l;
			int _i;
			T _v;

			public Enumerator(T[] l)
			{
				_l = l;
				_i = -1;
				_v = default(T);
			}

			public bool MoveNext()
			{
				if (++_i >= _l.Length)
					return false;
				_v = _l[_i];
				return true;
			}

			public void Reset() => _i = -1;

			public T Current => _v;

			object IEnumerator.Current => _v;

			void IDisposable.Dispose()
			{
			}
		}
	}

	/// <summary>
	/// Strings extensions.
	/// </summary>
	public static class StringExtensions
	{
		public static byte[] ToByteArray(this string src)
		{
			return Encoding.UTF8.GetBytes(src);
		}

		public static byte[] FromHexString(this string src)
		{
			return ConvertEx.FromHexString(src);
		}

		public static MemoryStream ToMemoryStream(this string src)
		{
			return ConvertEx.ToMemoryStream(src);
		}
	}

	/// <summary>
	/// Collections extensions.
	/// </summary>
	public static class CollectionExtensions
	{
		public static void Sort<T>(this ObservableCollection<T> observableCollection, Comparison<T> comparison)
		{
			var l = new List<T>(observableCollection);
			if (comparison == null)
				l.Sort();
			else
				l.Sort(comparison);
			
			for (var i = 0; i < l.Count; i++)
			{
				var oldIndex = observableCollection.IndexOf(l[i]);
				var newIndex = i;
				if (oldIndex != newIndex)
					observableCollection.Move(oldIndex, newIndex);
			}
		}
	}

}
