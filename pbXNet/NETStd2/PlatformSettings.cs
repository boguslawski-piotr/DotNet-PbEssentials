using System.IO;
using System.Reflection;
using System.Threading.Tasks;

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
		IFileSystem _fs;
		IFileSystem _Fs
		{
			get {
				if (_fs == null)
				{
					_fs = DeviceFileSystem.New(DeviceFileSystem.RootType.RoamingConfig);
					try
					{
						string dirname = this.GetType().GetTypeInfo().Assembly.ManifestModule.Name;
						Task.Run(() => _fs.CreateDirectoryAsync(dirname)).GetAwaiter().GetResult();
					}
					catch { }
				}
				return _fs;
			}
		}
#endif

		async Task<string> _GetStringAsync(string id)
		{
#if !NETSTANDARD1_6
			using (var stream = _store.OpenFile(id, FileMode.Open))
			{
				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
#else
			if (await _Fs.FileExistsAsync(id))
				return await _Fs.ReadTextAsync(id);
			return null;
#endif
		}

		async Task _SetStringAsync(string id, string d)
		{
#if !NETSTANDARD1_6
			using (var stream = _store.OpenFile(id, FileMode.Create, FileAccess.Write))
			{
				using (var writer = new StreamWriter(stream))
				{
					writer.Write(d);
				}
			}
#else
			await _Fs.WriteTextAsync(id, d);
#endif
		}
	}
}