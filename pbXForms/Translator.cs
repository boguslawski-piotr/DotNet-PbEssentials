using System;
using System.Diagnostics;
using System.Globalization;
using System.Resources;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace pbXForms
{
    public static class Translator
    {
        public static ResourceManager ResourceManager { set; get; }

        public static CultureInfo Culture { set; get; }

        private static Locale _locale = new Locale();

        public static CultureInfo CurrentCultureInfo
        {
            get {
                Culture = _locale.GetCurrentCultureInfo();
                _locale.SetLocale(Culture);
                return Culture;
            }
        }

        public static string T(string name)
        {
            if (name == null)
                return "";

            string value = null;

            if (ResourceManager != null)
                value = ResourceManager.GetString(name, Culture);

            if (value == null)
            {
                value = $"!@ {name} @!"; // returns the key, which GETS DISPLAYED TO THE USER
            }

            return value;
        }
    }

    // You exclude the 'Extension' suffix when using in Xaml markup
    [ContentProperty("Text")]
    public class TranslatorExtension : IMarkupExtension
    {
        public string Text { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return Translator.T(Text);
        }
    }

}
