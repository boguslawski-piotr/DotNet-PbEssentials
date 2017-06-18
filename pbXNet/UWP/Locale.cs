#if WINDOWS_UWP

using System;
using System.Globalization;

namespace pbXNet
{
	public partial class Locale : ILocale
	{
        // Windows 8.1 and Universal Windows Platform (UWP) projects do not require the dependency service – these platforms automatically set the resource's culture correctly.

		CultureInfo _GetCurrentCultureInfo()
		{
			return null;
		}

		void _SetLocale(CultureInfo ci)
		{
		}
	}
}

#endif
