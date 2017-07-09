using System;
using Plugin.pbXSettings.Abstractions;

namespace Plugin.pbXSettings
{
	public static class SettingsStorage
	{
		/// <summary>
		/// 
		/// </summary>
		public static bool IsSupported => _impl.Value == null ? false : true;

		/// <summary>
		/// 
		/// </summary>
		public static ISettingsStorage Current
		{
			get {
				ISettingsStorage ss = _impl.Value;
				if (ss == null)
					throw NotImplementedInReferenceAssembly();
				return ss;
			}
		}

		static Lazy<ISettingsStorage> _impl = new Lazy<ISettingsStorage>(() => CreateSettingsStorage(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

		static ISettingsStorage CreateSettingsStorage()
		{
#if __PCL__
            return null;
#else
			return (ISettingsStorage)new SettingsStorageImplementation();
#endif
		}

		internal static Exception NotImplementedInReferenceAssembly() =>
			new NotImplementedException("This functionality is not implemented in the portable version of this assembly. You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
	}
}
