using System;
using Plugin.pbXSettings.Abstractions;

namespace Plugin.pbXSettings
{
	public static class SettingsStorage
	{
		static Lazy<ISettingsStorage> implementation = new Lazy<ISettingsStorage>(() => CreateSettingsStorage(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

		public static bool IsSupported => implementation.Value == null ? false : true;

		public static ISettingsStorage Current
		{
			get {
				ISettingsStorage ret = implementation.Value;
				if (ret == null)
				{
					throw NotImplementedInReferenceAssembly();
				}
				return ret;
			}
		}

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
