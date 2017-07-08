using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Preferences;

namespace pbXNet
{
	public partial class PlatformSettings : Settings
	{
		async Task<string> _GetStringAsync(string id)
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

		async Task _SetStringAsync(string id, string d)
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