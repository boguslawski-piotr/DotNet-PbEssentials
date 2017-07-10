using System.Threading.Tasks;

namespace Plugin.pbXSettings.Abstractions
{
	/// <summary>
	/// An interface that defines methods for accessing a native settings storage.
	/// </summary>
	public interface ISettingsStorage
    {
		/// <summary>
		/// Should retrieve a string described by identifier <paramref name="id"/> from native settings storage.
		/// </summary>
		Task<string> GetStringAsync(string id);

		/// <summary>
		/// Should store string <paramref name="d"/> in native settings storage giving it identifier <paramref name="id"/>.
		/// </summary>
		Task SetStringAsync(string id, string d);
    }
}