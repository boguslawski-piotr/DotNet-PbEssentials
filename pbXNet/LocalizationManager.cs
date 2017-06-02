using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;

//using Xamarin.Forms;
//using Xamarin.Forms.Xaml;

namespace pbXNet
{
    public static class LocalizationManager
    {
        static Locale _Locale = new Locale();

        public static CultureInfo CurrentCultureInfo
        {
            get {
                CultureInfo c = _Locale.GetCurrentCultureInfo();
                _Locale.SetLocale(c);
                return c;
            }
        }

        static CultureInfo _Culture;

        public static CultureInfo Culture
        {
            get {
                if (_Culture == null)
                    _Culture = CurrentCultureInfo;
                return _Culture;
            }
            set {
                _Culture = value;
            }
        }

        class Resource
        {
            public string BaseName { get; set; }
            public Assembly Assembly { get; set; }

            ResourceManager _ResourceManager;
            public ResourceManager ResourceManager
            {
                get {
                    if (object.ReferenceEquals(_ResourceManager, null))
                    {
                        ResourceManager rm = new ResourceManager(BaseName, Assembly);
                        _ResourceManager = rm;
                    }
                    return _ResourceManager;
                }

            }
        }

        static IList<Resource> _resources = new List<Resource>();

        public static void AddResource(string baseName, Assembly assembly)
        {
            Resource resource = new Resource()
            {
                BaseName = baseName,
                Assembly = assembly,
            };

            _resources.Add(resource);
        }

        public static string GetText(string name)
        {
            if (name == null)
                return "";

            string value = null;

            if (_resources.Count > 0)
            {
                foreach (var r in _resources)
                {
                    try
                    {
                        value = r.ResourceManager.GetString(name, Culture);
                        if (value != null)
                            break;
                    }
                    catch { }
                }
            }

            if (value == null)
                value = $"!@ {name} @!"; // returns the key, which GETS DISPLAYED TO THE USER

            return value;
        }
    }

    /// <summary>
    /// An auxiliary class used to the intuitive use of localized text in the code.
    /// Example: string s = T.Localized("text id");
    /// </summary>
    public static class T
    {
        public static string Localized(string name)
        {
            return LocalizationManager.GetText(name);
        }
    }
}
