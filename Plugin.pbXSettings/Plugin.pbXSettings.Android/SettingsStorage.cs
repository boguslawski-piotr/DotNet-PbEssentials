using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Preferences;
using Plugin.pbXSettings.Abstractions;

namespace Plugin.pbXSettings
{
	public class SettingsStorageImplementation : ISettingsStorage
	{
		public async Task<string> GetStringAsync(string id)
		{
			if (Application.Context == null)
				return null;
			using (var _preferences = PreferenceManager.GetDefaultSharedPreferences(Application.Context))
			{
				if (_preferences.Contains(id))
					return _preferences.GetString(id, null);
				return null;
			}
		}

		public async Task SetStringAsync(string id, string d)
		{
			using (var _preferences = PreferenceManager.GetDefaultSharedPreferences(Application.Context))
			{
				using (var _preferencesEditor = _preferences.Edit())
				{
					_preferencesEditor.PutString(id, d);
					_preferencesEditor.Commit();
				}
			}
		}
	}
}