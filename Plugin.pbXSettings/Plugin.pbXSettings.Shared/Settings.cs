using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Plugin.pbXSettings
{
	public class Settings : pbXNet.Settings
	{
		/// <summary>
		/// 
		/// </summary>
		public static Settings Current => _current.Value;
		static readonly Lazy<Settings> _current = new Lazy<Settings>(() => new Settings(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

		/// <summary>
		/// 
		/// </summary>
		public static bool Initialize()
		{
			return SettingsStorage.IsSupported;
		}

		/// <summary>
		/// 
		/// </summary>
		public string Id { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		protected readonly pbXNet.ISerializer Serializer = new pbXNet.DataContractSerializer();

		const string _defaultId = ".a0d40b25942b4788904532af03886608";

		/// <summary>
		/// 
		/// </summary>
		public Settings()
		{
			Id = _defaultId;
			Load();
		}

		/// <summary>
		/// 
		/// </summary>
		public Settings(string id)
		{
			Id = id ?? _defaultId;
			Load();
		}

		/// <summary>
		/// 
		/// </summary>
		public override async Task LoadAsync()
		{
			string d = await SettingsStorage.Current.GetStringAsync(Id);
			if (!string.IsNullOrWhiteSpace(d))
			{
				d = pbXNet.Obfuscator.DeObfuscate(d);
				KeysAndValues = Serializer.Deserialize<ConcurrentDictionary<string, object>>(d);
			}
			else
				KeysAndValues?.Clear();
		}

		/// <summary>
		/// 
		/// </summary>
		public override async Task SaveAsync(string changedValueKey = null)
		{
			string d = Serializer.Serialize(KeysAndValues);
			if (d != null)
			{
				d = pbXNet.Obfuscator.Obfuscate(d);
				await SettingsStorage.Current.SetStringAsync(Id, d);
			}
		}
	}
}
