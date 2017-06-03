using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace pbXNet
{
	public static class LocalizationManager
	{
		static Locale _locale = new Locale();

		public static CultureInfo CurrentCultureInfo
		{
			get {
				CultureInfo c = _locale.GetCurrentCultureInfo();
				_locale.SetLocale(c);
				return c;
			}
		}

		static CultureInfo _cultureInfo;

		public static CultureInfo CultureInfo
		{
			get {
				if (_cultureInfo == null)
					_cultureInfo = CurrentCultureInfo;
				return _cultureInfo;
			}
			set {
				_cultureInfo = value;
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

		static readonly IList<Resource> _resources = new List<Resource>();

		public static void AddResource(string baseName, Assembly assembly, bool first = false)
		{
			if (first)
			{
				_resources.Clear();
				CultureInfo = null;
			}

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
						value = r.ResourceManager.GetString(name, CultureInfo);
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
