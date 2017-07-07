using System;
using System.Globalization;
using System.Threading;

namespace pbXNet
{
	public partial class Locale : ILocale
	{
		public void _SetLocale(CultureInfo ci)
		{
			Thread.CurrentThread.CurrentCulture = ci;
			Thread.CurrentThread.CurrentUICulture = ci;
		}

		public CultureInfo _GetCurrentCultureInfo()
		{
			return CultureInfo.CurrentUICulture;
		}
	}
}
