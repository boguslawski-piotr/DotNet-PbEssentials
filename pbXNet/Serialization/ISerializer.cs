using System;

#if PLUGIN_PBXSETTINGS
namespace Plugin.pbXSettings.pbXNet
#else
namespace pbXNet
#endif
{
	public interface ISerializer
	{
		string Serialize<T>(T o, string id = null);
		T Deserialize<T>(string d, string id = null);
	}
}
