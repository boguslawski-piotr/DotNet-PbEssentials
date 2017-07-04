//#if WINDOWS_UWP

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace pbXNet
{
	// Because the API for Windows UWP does not provide any method 
	// for modifying the date of the last modification of the file, and 
	// SetFileModifiedOnAsync / GetFileModifiedOnAsync functions are 
	// really needed in other parts of this library,
	// I had to implement it with a separate mechanism ;)
	//
	// This is not the best solution, it should be done using the system API. 
	// I hope Microsoft will expand its API in the future.
	//

	// Usefull links:
	// https://blogs.windows.com/buildingapps/2016/05/10/getting-started-storing-app-data-locally/

	public partial class DeviceFileSystem : IFileSystem, IDisposable
	{
		public static readonly IEnumerable<DeviceFileSystemRoot> AvailableRootsForEndUser = new List<DeviceFileSystemRoot>()
		{
			DeviceFileSystemRoot.Local,
			DeviceFileSystemRoot.Roaming,
		};

		StorageFolder _root;
		StorageFolder _current;

		public string RootPath => _root?.Path;
		public string CurrentPath => _current?.Path;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

		public virtual void Initialize()
		{
			switch (Root)
			{
				case DeviceFileSystemRoot.UserDefined:
					throw new NotSupportedException();

				case DeviceFileSystemRoot.RoamingConfig:
				case DeviceFileSystemRoot.Roaming:
					_root = ApplicationData.Current.RoamingFolder;
					break;

				case DeviceFileSystemRoot.LocalConfig:
				case DeviceFileSystemRoot.Local:
				default:
					_root = ApplicationData.Current.LocalFolder;
					break;
			}

			_current = _root;
			InitializeAsync();
		}

		async Task InitializeAsync()
		{
			if (Root == DeviceFileSystemRoot.LocalConfig || Root == DeviceFileSystemRoot.RoamingConfig)
			{
				await CreateDirectoryAsync(".config").ConfigureAwait(false);
				_root = _current;
			}

			LoadModifiedOnDictAsync(_root);
		}

#pragma warning restore CS4014

		public virtual void Dispose()
		{
			_current = null;
			_root = null;
		}

		public virtual async Task<IFileSystem> CloneAsync()
		{
			DeviceFileSystem cp = new DeviceFileSystem(this.Root);
			if (_root != null)
				cp._root = await StorageFolder.GetFolderFromPathAsync(_root.Path);
			if (_current != null)
				cp._current = await StorageFolder.GetFolderFromPathAsync(_current.Path);

			return cp;
		}

		static readonly string _modifiedOnDictFileName = ".3c96f3db6d304e1b984476dc380e5aea";

		static readonly ISerializer _serializer = new NewtonsoftJsonSerializer();

		static IDictionary<string, IDictionary<string, DateTime>> _modifiedOnDictDict = new Dictionary<string, IDictionary<string, DateTime>>();

		static readonly SemaphoreSlim _saveModifiedOnDictLock = new SemaphoreSlim(1);

		static Int32 _saveModifiedOnDictTaskRunning = 0;

		string FileNameForModifiedOn(string filename)
		{
			string path = _current.Path.Substring(Math.Min(_current.Path.Length, _root.Path.Length + 1));
			return Path.Combine(path, filename);
		}

		static bool TryGetModifiedOn(StorageFolder root, string filename, out DateTime modifiedOn)
		{
			_saveModifiedOnDictLock.Wait();
			try
			{
				if (_modifiedOnDictDict.TryGetValue(root.Path, out IDictionary<string, DateTime> modifiedOnDict))
					if (modifiedOnDict.TryGetValue(filename, out modifiedOn))
						return true;

				modifiedOn = DateTime.MinValue;
				return false;
			}
			finally
			{
				_saveModifiedOnDictLock.Release();
			}
		}

		static void ModifyModifiedOnDict(StorageFolder root, Action<IDictionary<string, DateTime>> action)
		{
			_saveModifiedOnDictLock.Wait();
			try
			{
				if (_modifiedOnDictDict.TryGetValue(root.Path, out IDictionary<string, DateTime> modifiedOnDict))
					action(modifiedOnDict);
			}
			finally
			{
				_saveModifiedOnDictLock.Release();
			}
		}

		static async Task LoadModifiedOnDictAsync(StorageFolder root)
		{
			await _saveModifiedOnDictLock.WaitAsync();
			try
			{
				if (_modifiedOnDictDict.ContainsKey(root.Path))
					return;

				IStorageItem storageFile = await root.TryGetItemAsync(_modifiedOnDictFileName);
				if (storageFile != null)
				{
					string d = await FileIO.ReadTextAsync(storageFile as StorageFile);
					d = Obfuscator.DeObfuscate(d);
					_modifiedOnDictDict[root.Path] = _serializer.Deserialize<IDictionary<string, DateTime>>(d);
				}
			}
			finally
			{
				_saveModifiedOnDictLock.Release();
			}
		}

		static async Task SaveModifiedOnDictAsync(StorageFolder root)
		{
			DateTime sdt = DateTime.Now;

			await _saveModifiedOnDictLock.WaitAsync();
			try
			{
				if (_modifiedOnDictDict.TryGetValue(root.Path, out IDictionary<string, DateTime> modifiedOnDict))
				{
					IStorageFile storageFile = await root.CreateFileAsync(_modifiedOnDictFileName, CreationCollisionOption.ReplaceExisting);
					string d = _serializer.Serialize(modifiedOnDict);
					d = Obfuscator.Obfuscate(d);
					await FileIO.WriteTextAsync(storageFile, d);
				}
			}
			catch (Exception ex)
			{
				Log.E(ex.Message);
			}
			finally
			{
				Interlocked.Exchange(ref _saveModifiedOnDictTaskRunning, 0);
				_saveModifiedOnDictLock.Release();

				Log.D($"duration: {(DateTime.Now - sdt).Milliseconds} for: {root.Path}");
			}
		}

		static void ScheduleSaveModifiedOnDict(StorageFolder root)
		{
			if (Interlocked.Exchange(ref _saveModifiedOnDictTaskRunning, 1) == 1)
				return;

			Task.Run((async () =>
			{
				await Task.Delay(1000);
				await SaveModifiedOnDictAsync((StorageFolder)root);
			}));
		}

		public static async Task SaveAllModifiedOnDictsAsync()
		{
			foreach(var rootPath in _modifiedOnDictDict.Keys.ToArray())
			{
				StorageFolder root = await StorageFolder.GetFolderFromPathAsync(rootPath);
				await SaveModifiedOnDictAsync(root);
			}
		}

		class _State
		{
			public StorageFolder savedRoot;
			public StorageFolder savedCurrent;
		};

		Stack<_State> _stateStack = new Stack<_State>();

		public virtual Task SaveStateAsync()
		{
			_stateStack.Push(new _State
			{
				savedRoot = _root,
				savedCurrent = _current,
			});

			return Task.FromResult(true);
		}

		public virtual Task RestoreStateAsync()
		{
			if (_stateStack.Count > 0)
			{
				_State state = _stateStack.Pop();
				_root = state.savedRoot;
				_current = state.savedCurrent;
			}

			return Task.FromResult(true);
		}


		public virtual async Task SetCurrentDirectoryAsync(string dirname)
		{
			if (dirname == null || dirname == "")
			{
				_current = _root;
			}
			else if (dirname == "..")
			{
				if (_current.Path != _root.Path)
					_current = await _current.GetParentAsync();
			}
			else
			{
				_current = await _current.GetFolderAsync(dirname);
			}
		}

		public virtual async Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "")
		{
			IEnumerable<string> dirnames =
				from storageDir in await _current.GetFoldersAsync()
				where Regex.IsMatch(storageDir.Name, pattern)
				select storageDir.Name;
			return dirnames;
		}

		public virtual async Task<bool> DirectoryExistsAsync(string dirname)
		{
			return await _current.TryGetItemAsync(dirname) != null;
		}

		public virtual async Task CreateDirectoryAsync(string dirname)
		{
			if (!string.IsNullOrEmpty(dirname))
				_current = await _current.CreateFolderAsync(dirname, CreationCollisionOption.OpenIfExists);
		}

		public virtual async Task DeleteDirectoryAsync(string dirname)
		{
			try
			{
				IStorageFolder storageDir = await _current.GetFolderAsync(dirname);
				await storageDir.DeleteAsync();
			}
			catch (FileNotFoundException) { }
		}


		public virtual async Task<IEnumerable<string>> GetFilesAsync(string pattern = "")
		{
			IEnumerable<string> filenames =
				from storageFile in await _current.GetFilesAsync()
				where Regex.IsMatch(storageFile.Name, pattern)
				select storageFile.Name;
			return filenames;
		}

		public virtual async Task<bool> FileExistsAsync(string filename)
		{
			return await _current.TryGetItemAsync(filename) != null;
		}

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

		public virtual async Task DeleteFileAsync(string filename)
		{
			try
			{
				IStorageFile storageFile = await _current.GetFileAsync(filename);
				await storageFile.DeleteAsync();

				ModifyModifiedOnDict(_root, (modifiedOnDict) => modifiedOnDict.Remove(FileNameForModifiedOn(filename)));
				ScheduleSaveModifiedOnDict(_root);
			}
			catch (FileNotFoundException) { }
		}

		public virtual async Task SetFileModifiedOnAsync(string filename, DateTime modifiedOn)
		{
			ModifyModifiedOnDict(_root, (modifiedOnDict) => modifiedOnDict[FileNameForModifiedOn(filename)] = modifiedOn.ToUniversalTime());
			ScheduleSaveModifiedOnDict(_root);
		}

#pragma warning restore CS4014

		public virtual async Task<DateTime> GetFileModifiedOnAsync(string filename)
		{
			try
			{
				if (TryGetModifiedOn(_root, FileNameForModifiedOn(filename), out DateTime modifiedOn))
					return modifiedOn;

				IStorageFile storageFile = await _current.GetFileAsync(filename);
				BasicProperties props = await storageFile.GetBasicPropertiesAsync();
				return props.DateModified.DateTime.ToUniversalTime();
			}
			catch (FileNotFoundException) { }
			return DateTime.MinValue;
		}

		public virtual async Task WriteTextAsync(string filename, string text)
		{
			IStorageFile storageFile = await _current.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
			await FileIO.WriteTextAsync(storageFile, text);
		}

		public virtual async Task<string> ReadTextAsync(string filename)
		{
			IStorageFile storageFile = await _current.GetFileAsync(filename);
			return await FileIO.ReadTextAsync(storageFile);
		}
	}
}

//#endif
