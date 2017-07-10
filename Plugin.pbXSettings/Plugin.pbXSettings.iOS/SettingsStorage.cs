using System.Threading.Tasks;
using Foundation;
using Plugin.pbXSettings.Abstractions;

namespace Plugin.pbXSettings
{
	public class SettingsStorageImplementation : ISettingsStorage
	{
		public async Task<string> GetStringAsync(string id)
		{
			var defaults = NSUserDefaults.StandardUserDefaults;
			defaults.Synchronize();

			if (defaults[id] != null)
				return defaults.StringForKey(id);
			return null;
		}

		public async Task SetStringAsync(string id, string d)
		{
			var defaults = NSUserDefaults.StandardUserDefaults;
			defaults.SetString(d, id);
			defaults.Synchronize();
		}
	}
}