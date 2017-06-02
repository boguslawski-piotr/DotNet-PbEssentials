#if WINDOWS_UWP

using System;

namespace pbXNet
{

	public partial class Locale : ILocale
	{
        // Windows 8.1 and Universal Windows Platform (UWP) projects do not require the dependency service – these platforms automatically set the resource's culture correctly.

		public CultureInfo GetCurrentCultureInfo()
		{
			return null;
		}

		public void SetLocale(CultureInfo ci)
		{
		}
	}
}

#endif
