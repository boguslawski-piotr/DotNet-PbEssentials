#if __UNIFIED__

using System.Globalization;
using System.Threading;
using Foundation;

namespace pbXNet
{
	public partial class Locale : ILocale
	{
		void _SetLocale(CultureInfo ci)
		{
			Thread.CurrentThread.CurrentCulture = ci;
			Thread.CurrentThread.CurrentUICulture = ci;
		}

#pragma warning disable CS0168

		CultureInfo _GetCurrentCultureInfo()
		{
			var netLanguage = "en";

			if (NSLocale.PreferredLanguages.Length > 0)
			{
				var pref = NSLocale.PreferredLanguages[0];
				netLanguage = iOSToDotnetLanguage(pref);
			}

			System.Globalization.CultureInfo ci = null;
			try
			{
				ci = new System.Globalization.CultureInfo(netLanguage);
			}
			catch (CultureNotFoundException e1)
			{
				// locale not valid .NET culture (eg. "en-ES" : English in Spain)
				// fallback to first characters, in this case "en"
				try
				{
					var fallback = ToDotnetFallbackLanguage(new PlatformCulture(netLanguage));
					ci = new System.Globalization.CultureInfo(fallback);
				}
				catch (CultureNotFoundException e2)
				{
					// language not valid .NET culture, falling back to English
					ci = new System.Globalization.CultureInfo("en");
				}
			}

			return ci;
		}
		
#pragma warning restore CS0168
		
		string iOSToDotnetLanguage(string iOSLanguage)
		{
			var netLanguage = iOSLanguage;

			switch (iOSLanguage)
			{
				case "ms-MY":   // "Malaysian (Malaysia)" not supported .NET culture
				case "ms-SG":   // "Malaysian (Singapore)" not supported .NET culture
					netLanguage = "ms";
					break;
				case "gsw-CH":  // "Schwiizertüütsch (Swiss German)" not supported .NET culture
					netLanguage = "de-CH";
					break;

					// add more application-specific cases here (if required)
					// ONLY use cultures that have been tested and known to work
			}

			return netLanguage;
		}

		string ToDotnetFallbackLanguage(PlatformCulture platCulture)
		{
			var netLanguage = platCulture.LanguageCode;

			switch (platCulture.LanguageCode)
			{
				case "pt":
					netLanguage = "pt-PT"; // fallback to Portuguese (Portugal)
					break;
				case "gsw":
					netLanguage = "de-CH"; // equivalent to German (Switzerland) for this app
					break;

					// add more application-specific cases here (if required)
					// ONLY use cultures that have been tested and known to work
			}

			return netLanguage;
		}
	}
}

#endif
