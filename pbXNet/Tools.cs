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
		public static bool MakeIdenticalIfDifferent<T>(T a, ref T b)
		{
			if (Equals(a, b))
				return false;
			b = a;
			return true;
		}

		/// <summary>
		/// Creates the GUID (Global Unique IDentifier) in the most compact form.
		/// </summary>
		public static string CreateGuid()
		{
			return System.Guid.NewGuid().ToString("N");
		}

		/// <summary>
		/// Gets the Uaqpid (Unique And Quite Permanent IDentifier ;)) (should be really unique accross all devices with the same operating system). 
		/// which will be quite constant but there is no guarantee that this Id will be the same for all eternity.
		/// It may change after uninstalling the application or after reinstalling the operating system.
		/// It is different for different users on some systems.
		/// It is different for different applications that use this library.
		/// TIP: This function could be slow and it is recommended to store the results in a local/class variable.
		/// </summary>
		public static string GetUaqpid()
		{
			byte[] id = new AesCryptographer().GenerateKey(Encoding.UTF8.GetBytes(_Uaqpid), new byte[] { 34, 56, 2, 34, 6, 87, 12, 34, 56, 11 });
			return ConvertEx.ToHexString(id);
		}
	}
}
