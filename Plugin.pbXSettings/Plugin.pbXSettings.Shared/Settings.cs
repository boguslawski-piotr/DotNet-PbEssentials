using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Plugin.pbXSettings
{
	public class Settings : pbXNet.Settings
	{
		/// <summary>
		/// Default settings set.
		/// </summary>
		public static Settings Current => _current.Value;
		static readonly Lazy<Settings> _current = new Lazy<Settings>(() => new Settings(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

		const string _defaultId = ".a0d40b25942b4788904532af03886608";

		/// <summary>
		/// Settings set identifier. It can be any string as long as can be used as file name.
		/// </summary>
		public string Id { get; private set; }

		/// <summary>
		/// Current serializer.
		/// Default: <see cref="pbXNet.DataContractSerializer"/>
		/// </summary>
		protected readonly pbXNet.ISerializer Serializer = new pbXNet.DataContractSerializer();

		/// <summary>
		/// Constructor for default settings set.
		/// </summary>
		public Settings()
		{
			Id = _defaultId;
			Load();
		}

		/// <summary>
		/// Constructor for user defined settings set. 
		/// Parameter <paramref name="id"/> can be any string as long as can be used as file name.
		/// </summary>
		public Settings(string id)
		{
			Id = id ?? _defaultId;
			Load();
		}

		/// <summary>
		/// Loads entire settings set using native settings storage.
		/// </summary>
		/// <seealso cref="SettingsStorage"/>
		/// <seealso cref="Abstractions.ISettingsStorage"/>
		public override async Task LoadAsync()
		{
			string d = await SettingsStorage.Current.GetStringAsync(Id);
			if (!string.IsNullOrWhiteSpace(d))
			{
				d = pbXNet.Obfuscator.DeObfuscate(d);
				Deserialize(d);
			}
			else
				KeysAndValues?.Clear();
		}

		/// <summary>
		/// Saves entire settings set using native settings storage.
		/// </summary>
		/// <seealso cref="SettingsStorage"/>
		/// <seealso cref="Abstractions.ISettingsStorage"/>
		public override async Task SaveAsync(string changedValueKey = null)
		{
			string d = Serialize();
			if (d != null)
			{
				d = pbXNet.Obfuscator.Obfuscate(d);
				await SettingsStorage.Current.SetStringAsync(Id, d);
			}
		}

		/// <summary>
		/// Serializes <see cref="pbXNet.Settings.KeysAndValues"/> dictionary to string.
		/// </summary>
		protected virtual string Serialize()
		{
			return Serializer.Serialize(KeysAndValues);
		}

		/// <summary>
		/// Deserializes <see cref="pbXNet.Settings.KeysAndValues"/> dictionary from string.
		/// </summary>
		protected virtual void Deserialize(string d)
		{
			KeysAndValues = Serializer.Deserialize<ConcurrentDictionary<string, object>>(d);
		}

		/// <summary>
		/// Should return true for properties which are not settings and should not be seen when enumerating settings set.
		/// </summary>
		protected override bool IsInternalProperty(string name)
		{
			if (name == nameof(Id))
				return true;
			return base.IsInternalProperty(name);
		}
	}
}
