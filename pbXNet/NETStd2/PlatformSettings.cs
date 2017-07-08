using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System;

#if !NETSTANDARD1_6
using System.IO.IsolatedStorage;
#endif

namespace pbXNet
{
	public partial class PlatformSettings : Settings
	{
#if !NETSTANDARD1_6
		static IsolatedStorageFile _store => IsolatedStorageFile.GetUserStoreForDomain();
#else
		static IFileSystem _fs = DeviceFileSystem.New(DeviceFileSystem.RootType.RoamingConfig);
#endif

		string FileName(string id) => $"{id}-{string.Format("{0,8:X}", id.GetHashCode())}";

		async Task<string> _GetStringAsync(string id)
		{
			string filename = FileName(id);

#if !NETSTANDARD1_6
			if (_store.FileExists(filename))
			{
				using (var stream = _store.OpenFile(filename, FileMode.Open))
				using (var reader = new StreamReader(stream))
				{
					return await reader.ReadToEndAsync();
				}
			}
#else
			if (await _fs.FileExistsAsync(filename))
				return await _fs.ReadTextAsync(filename);
#endif

			return null;
		}

		async Task _SetStringAsync(string id, string d)
		{
			string filename = FileName(id);

#if !NETSTANDARD1_6
			using (var stream = _store.OpenFile(filename, FileMode.Create, FileAccess.Write))
			using (var writer = new StreamWriter(stream))
			{
				await writer.WriteAsync(d);
			}
#else
			await _fs.WriteTextAsync(filename, d);
#endif
		}
	}
}