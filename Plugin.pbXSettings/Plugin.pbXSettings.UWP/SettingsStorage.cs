using System.Threading.Tasks;
using Windows.Storage;
using Plugin.pbXSettings.Abstractions;

namespace Plugin.pbXSettings
{
	public class SettingsStorageImplementation : ISettingsStorage
	{
		ApplicationDataContainer _settings => ApplicationData.Current.RoamingSettings;

		public async Task<string> GetStringAsync(string id)
		{
			if (_settings.Values.ContainsKey(id))
				return (string)_settings.Values[id];
			return null;
		}

		public async Task SetStringAsync(string id, string d)
		{
			_settings.Values[id] = d;
		}
	}
}