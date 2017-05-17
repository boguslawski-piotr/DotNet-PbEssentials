using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;

namespace pbXForms
{
    public interface ILocale
    {
        CultureInfo GetCurrentCultureInfo();
        void SetLocale(CultureInfo ci);
    }

    public class Locale : ILocale
    {
        // TODO: implement Droid, iOS, macOS (https://developer.xamarin.com/guides/xamarin-forms/advanced/localization/)

#if WINDOWS_UWP

        public System.Globalization.CultureInfo GetCurrentCultureInfo()
        {
            // Windows 8.1 and Universal Windows Platform (UWP) projects do not require the dependency service – these platforms automatically set the resource's culture correctly.
            return null;
        }

        public void SetLocale(CultureInfo ci)
        {
            // Windows 8.1 and Universal Windows Platform (UWP) projects do not require the dependency service – these platforms automatically set the resource's culture correctly.
        }
#endif

#if __ANDROID__

        public System.Globalization.CultureInfo GetCurrentCultureInfo()
        {
            return new CultureInfo("pl");
        }

        public void SetLocale(CultureInfo ci)
        {
        }

#endif

#if __IOS__ || __UNIFIED__

        public System.Globalization.CultureInfo GetCurrentCultureInfo()
        {
            return new CultureInfo("pl");
        }

        public void SetLocale(CultureInfo ci)
        {
        }

#endif

    }



}

