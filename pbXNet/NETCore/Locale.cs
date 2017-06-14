using System;
using System.Globalization;

namespace pbXNet
{
	public partial class Locale : ILocale
	{
		public CultureInfo GetCurrentCultureInfo()
		{
			return null;
		}

		public void SetLocale(CultureInfo ci)
		{
		}
	}
}
