using System;
using System.Threading.Tasks;
using Foundation;

namespace pbXNet
{
	// TODO: handle iCloud
	// https://developer.apple.com/library/content/documentation/Cocoa/Conceptual/UserDefaults/StoringPreferenceDatainiCloud/StoringPreferenceDatainiCloud.html#//apple_ref/doc/uid/10000059i-CH7-SW1

	public partial class PlatformSettings : Settings
	{
		async Task<string> _GetStringAsync(string id)
		{
			var defaults = NSUserDefaults.StandardUserDefaults;
			defaults.Synchronize();

			if (defaults[id] != null)
				return defaults.StringForKey(id);
			return null;
		}

		async Task _SetStringAsync(string id, string d)
		{
			var defaults = NSUserDefaults.StandardUserDefaults;
			defaults.SetString(d, id);
		}
	}
}