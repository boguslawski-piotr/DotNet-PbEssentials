using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using Plugin.pbXSettings.Abstractions;

namespace Plugin.pbXSettings
{
	public class SettingsStorageImplementation : ISettingsStorage
	{
		static IsolatedStorageFile _store => IsolatedStorageFile.GetUserStoreForDomain();
		string FileName(string id) => $"{id}-{string.Format("{0,8:X}", id.GetHashCode())}";

		public async Task<string> GetStringAsync(string id)
		{
			string filename = FileName(id);

			if (_store.FileExists(filename))
			{
				using (var stream = _store.OpenFile(filename, FileMode.Open))
				using (var reader = new StreamReader(stream))
				{
					return await reader.ReadToEndAsync();
				}
			}

			return null;
		}

		public async Task SetStringAsync(string id, string d)
		{
			string filename = FileName(id);

			using (var stream = _store.OpenFile(filename, FileMode.Create, FileAccess.Write))
			using (var writer = new StreamWriter(stream))
			{
				await writer.WriteAsync(d);
			}
		}
	}
}