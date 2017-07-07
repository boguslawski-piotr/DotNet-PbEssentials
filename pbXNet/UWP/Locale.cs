using System.Globalization;

namespace pbXNet
{
	public partial class Locale : ILocale
	{
		// Windows 8.1 and Universal Windows Platform (UWP) platforms automatically set the resource's culture correctly.

		void _SetLocale(CultureInfo ci)
		{
		}

		CultureInfo _GetCurrentCultureInfo()
		{
			return null;
		}
	}
}
