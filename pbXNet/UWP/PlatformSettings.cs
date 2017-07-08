using System.Threading.Tasks;
using Windows.Storage;

namespace pbXNet
{
	public partial class PlatformSettings : Settings
	{
		ApplicationDataContainer _settings => ApplicationData.Current.RoamingSettings;
		//return ApplicationData.Current.LocalSettings;

		async Task<string> _GetStringAsync(string id)
		{
			if (_settings.Values.ContainsKey(id))
				return (string)_settings.Values[id];
			return null;
		}

		async Task _SetStringAsync(string id, string d)
		{
			_settings.Values[id] = d;
		}
	}
}