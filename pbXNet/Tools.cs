using Newtonsoft.Json;

namespace pbXNet
{
    /// <summary>
    /// Settings.
    /// </summary>
    static class Settings
    {
        /// <summary>
        /// Settings to use with JsonConvert.Serialize i Deserialize methods.
        /// </summary>
        internal static readonly JsonSerializerSettings JsonSerializer = new JsonSerializerSettings()
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore,
#if DEBUG
            Formatting = Formatting.Indented,
#endif
        };
    }


	/// <summary>
	/// Various useful functions.
	/// </summary>
	public static class Tools
    {
		/// <summary>
		/// Compares a with b and if they are identical then it returns false doing nothing. 
        /// When a and b are different then b becomes equal to a and the function returns true.
		/// </summary>
		public static bool IsDifferent<T>(T a, ref T b)
        {
            if (Equals(a, b))
                return false;
            b = a;
            return true;
        }

		/// <summary>
        /// Creates the GUID in the most compact form.
		/// </summary>
		public static string CreateGuid()
        {
            return System.Guid.NewGuid().ToString("N");
        }
    }
}
