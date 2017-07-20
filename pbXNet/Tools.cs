using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace pbXNet
{
	/// <summary>
	/// Various useful functions.
	/// </summary>
	public static partial class Tools
	{
		/// <summary>
		/// Compares a with b and if they are identical then it returns false doing nothing. 
		/// When a and b are different then b becomes equal to a and the function returns true.
		/// </summary>
		/// <example>
		/// <code>
		/// Size _osa;
		/// protected override void OnSizeAllocated(double width, double height)
		/// {
		///		base.OnSizeAllocated(width, height);
		///
		///		if (!Tools.MakeIdenticalIfDifferent(new Size(width, height), ref _osa))
		///			return;
		///		
		///		...handle OnSizeAllocated...
		///	 }
		/// </code>
		/// </example>
		public static bool MakeIdenticalIfDifferent<T>(T a, ref T b)
		{
			if (Equals(a, b))
				return false;
			b = a;
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		public static void Swap<T>(ref T a, ref T b)
		{
			T temp = a;
			a = b;
			b = temp;
		}

		/// <summary>
		/// Creates the GUID (Global Unique IDentifier) in the most compact form.
		/// </summary>
		public static string CreateGuid()
		{
			return System.Guid.NewGuid().ToString("N");
		}

		/// <summary>
		/// Creates the GUIDex (Global Unique IDentifier EXtended) in the most compact form.
		/// </summary>
		public static string CreateGuidEx()
		{
			return System.Guid.NewGuid().ToString("N") + DateTime.Now.Ticks.ToString();
		}

		/// <summary>
		/// Gets the Uaqpid (Unique And Quite Permanent IDentifier ;))
		/// which will be quite permanent and really unique accross all devices with the same operating system,
		/// but there is no guarantee that this Id will be the same for all eternity.
		/// </summary>
		/// <remarks>
		/// Uaqpid is different for different applications that use this library.
		/// It may change after uninstalling the application or after reinstalling the operating system.
		/// It is different for different users on some systems.
		/// <para>Default implementation uses:</para>
		/// <para>• iOS: <c>UIDevice.CurrentDevice.IdentifierForVendor</c></para>
		/// <para>• UWP: <c>Windows.Security.Credentials.PasswordVault</c> + <c>Guid</c></para>
		/// <para>• macOS: <c>Keychain</c> + <c>Guid</c></para>
		/// <para>• Android: <c>Android.Provider.Settings.Secure.AndroidId</c> + <c>AndroidKeyStore</c> + key created using <c>HmacSha256</c></para>
		/// <para>TIP: This function could be slow and it is recommended to store the results in a local/class variable.</para>
		/// </remarks>
		public static string GetUaqpid()
		{
			using (IPassword passwd = new Password(_Uaqpid))
			{
				using (IByteBuffer id = new AesCryptographer().GenerateKey(passwd, new ByteBuffer(new byte[] { 34, 56, 2, 34, 6, 87, 12, 34, 56, 11 })))
				{
					return id.ToString();
				}
			}
		}
	}

	public static class Check
	{
		/// <summary>
		/// 
		/// </summary>
		public static void Null(object o, string name, [CallerMemberName]string callerName = null)
		{
			if (o == null)
				throw new ArgumentNullException(name);
		}

		/// <summary>
		/// 
		/// </summary>
		public static void Empty(string s, string name, [CallerMemberName]string callerName = null)
		{
			if (string.IsNullOrWhiteSpace(s))
				throw new ArgumentNullException(name);
		}

		public static void True(bool expr, string message, string name, [CallerMemberName]string callerName = null)
		{
			if (!expr)
				throw new ArgumentException(message, name); // TODO: Check.True message
		}

		public static void False(bool expr, string message, string name, [CallerMemberName]string callerName = null)
		{
			if (expr)
				throw new ArgumentException(message, name); // TODO: Check.True message
		}
	}
}
