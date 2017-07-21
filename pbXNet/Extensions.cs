using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

#if PLUGIN_PBXSETTINGS
namespace Plugin.pbXSettings.pbXNet
#else
namespace pbXNet
#endif

{
	/// <summary>
	/// Arrays extensions.
	/// </summary>
	public static class ArrayExtensions
	{
		public static void FillWithDefault<VT>(this VT[] src) where VT : struct
		{
			for (int n = 0; n < src.Length; n++)
				src[n] = new VT();
		}

		public static void FillWithNull<NT>(this NT[] src) where NT : class
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
	/// Linq expression extensions.
	/// </summary>
	public static class ExpressionExtensions
	{
		public static PropertyInfo AsPropertyInfo<T, R>(this Expression<Func<T, R>> expr)
		{
			Expression body = expr.Body;
			if ((body as UnaryExpression)?.Operand is MemberExpression operand)
				body = operand;
			return (body as MemberExpression)?.Member as PropertyInfo;
		}
	}

	public static class ReflectionExtensions
	{
		public static IEnumerable<MemberInfo> GetRuntimePropertiesAndFields(this Type type)
		{
			return
				type.GetRuntimeProperties()
					.Select(p => (MemberInfo)p)
			.Concat(
				type.GetRuntimeFields()
					.Where(p => !p.CustomAttributes.Any(a => a.AttributeType.Name == nameof(CompilerGeneratedAttribute)))
					.Select(p => (MemberInfo)p)
				);
		}

		public static Type GetPropertyOrFieldType(this MemberInfo member)
		{
			if (member is PropertyInfo pi)
				return pi.PropertyType;
			else if (member is FieldInfo fi)
				return fi.FieldType;

			throw new ArgumentException(""); // TODO: exception message
		}

		public static object GetValue(this MemberInfo member, object obj)
		{
			if (member is PropertyInfo pi)
				return pi.GetValue(obj);
			else if (member is FieldInfo fi)
				return fi.GetValue(obj);

			throw new ArgumentException(""); // TODO: exception message
		}

		public static void SetValue(this MemberInfo member, object obj, object value)
		{
			if (member is PropertyInfo pi)
			{
				pi.SetValue(obj, value);
				return;
			}
			else if (member is FieldInfo fi)
			{
				fi.SetValue(obj, value);
				return;
			}

			throw new ArgumentException(""); // TODO: exception message
		}
	}
}
