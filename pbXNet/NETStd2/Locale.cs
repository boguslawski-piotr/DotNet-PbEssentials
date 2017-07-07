using System.Globalization;

namespace pbXNet
{
	public partial class Locale : ILocale
	{
		public void _SetLocale(CultureInfo ci)
		{
			CultureInfo.CurrentCulture = ci;
			CultureInfo.CurrentUICulture = ci;
			CultureInfo.DefaultThreadCurrentCulture = ci;
			CultureInfo.DefaultThreadCurrentUICulture = ci;
		}

		public CultureInfo _GetCurrentCultureInfo()
		{
			return CultureInfo.CurrentUICulture;
		}
	}
}
