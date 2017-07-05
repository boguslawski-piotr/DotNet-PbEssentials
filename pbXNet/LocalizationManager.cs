using System.Collections.Generic;
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

		static readonly List<Resource> _resources = new List<Resource>();
		static int _numberOfInstalledDefaultResources = 0;

		public static void AddResource(string baseName, Assembly assembly, bool first = false)
		{
			if (first)
			{
				_resources.Clear();
				CultureInfo = null;
				_numberOfInstalledDefaultResources = 0;
			}

			Resource resource = new Resource()
			{
				BaseName = baseName,
				Assembly = assembly,
			};

			_resources.Add(resource);
		}

		public static void AddDefaultResources()
		{
			if (_numberOfInstalledDefaultResources == 0)
			{
				AddResource("pbXNet.Exceptions.T", typeof(pbXNet.LocalizationManager).GetTypeInfo().Assembly);
				AddResource("pbXNet.Texts.T", typeof(pbXNet.LocalizationManager).GetTypeInfo().Assembly);
				_numberOfInstalledDefaultResources = 2;
			}
		}

		public static string Localized(string name, params string[] args)
		{
			if (name == null)
				return "";

			string value = null;

			AddDefaultResources();

			if (_resources.Count > 0)
			{
				foreach (var r in _resources)
				{
					try
					{
						value = r.ResourceManager.GetString(name, CultureInfo);
						if (value != null)
						{
							if (args.Length > 0)
								value = string.Format(value, args);
							break;
						}
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
	/// <para>Example:</para>
	/// <example>
	/// <code>string s = T.Localized("text id");</code>
	/// </example>
	/// </summary>
	public static class T
	{
		public static string Localized(string name, params string[] args)
		{
			return LocalizationManager.Localized(name, args);
		}
	}
}
