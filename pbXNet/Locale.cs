using System;
using System.Globalization;

namespace pbXNet
{
	public interface ILocale
	{
		CultureInfo GetCurrentCultureInfo();
		void SetLocale(CultureInfo ci);
	}

	public partial class Locale : ILocale
	{
		public CultureInfo GetCurrentCultureInfo()
		{
			return _GetCurrentCultureInfo();
		}

		public void SetLocale(CultureInfo ci)
		{
			_SetLocale(ci);
		}
	}

	internal class PlatformCulture
	{
		public PlatformCulture(string platformCultureString)
		{
			if (String.IsNullOrEmpty(platformCultureString))
			{
				throw new ArgumentException("Expected culture identifier.", nameof(platformCultureString));
			}

			PlatformString = platformCultureString.Replace("_", "-"); // .NET expects dash, not underscore
			var dashIndex = PlatformString.IndexOf("-", StringComparison.Ordinal);
			if (dashIndex > 0)
			{
				var parts = PlatformString.Split('-');
				LanguageCode = parts[0];
				LocaleCode = parts[1];
			}
			else
			{
				LanguageCode = PlatformString;
				LocaleCode = "";
			}
		}

		public string PlatformString { get; private set; }
		public string LanguageCode { get; private set; }
		public string LocaleCode { get; private set; }

		public override string ToString()
		{
			return PlatformString;
		}
	}
}

