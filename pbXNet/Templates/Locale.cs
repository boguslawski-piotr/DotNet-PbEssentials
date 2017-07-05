using System;
using System.Globalization;
using System.Threading;

namespace pbXNet
{
	public partial class Locale : ILocale
	{
		public void _SetLocale(CultureInfo ci)
		{
		}

		public CultureInfo _GetCurrentCultureInfo()
		{
			return CultureInfo.CurrentUICulture;
		}
	}
}
