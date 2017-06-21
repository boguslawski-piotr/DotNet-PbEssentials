#if WINDOWS_UWP

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
			DeviceFileSystemRoot.Personal,
			DeviceFileSystemRoot.Roaming,
		};

		StorageFolder _root;
		StorageFolder _current;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

		protected void Initialize()
		{
			switch (Root)
			{
				case DeviceFileSystemRoot.Roaming:
					_root = ApplicationData.Current.RoamingFolder;
					break;

				case DeviceFileSystemRoot.Config:
				default:
					_root = ApplicationData.Current.LocalFolder;
					break;
			}

			_current = _root;

			if (Root == DeviceFileSystemRoot.Config)
				InitializeConfigRootAsync();
			else
				InitializeAsync();
		}

		async Task InitializeConfigRootAsync()
		{
			await CreateDirectoryAsync(".config").ConfigureAwait(false);
			_root = _current;
			InitializeAsync();
		}

		async Task InitializeAsync()
		{
			LoadModifiedOnDictAsync();
		}

#pragma warning restore CS4014

		public void Dispose()
		{
			_modifiedOnDict?.Clear();
			_modifiedOnDict = null;
			_current = null;
			_root = null;
		}

		public async Task<IFileSystem> MakeCopyAsync()
		{
			DeviceFileSystem cp = new DeviceFileSystem(this.Root);
			if (_root != null)
				cp._root = await StorageFolder.GetFolderFromPathAsync(_root.Path);
			if (_current != null)
				cp._current = await StorageFolder.GetFolderFromPathAsync(_current.Path);

			await cp.LoadModifiedOnDictAsync().ConfigureAwait(false);

			return cp;
		}

		const string _modifiedOnDictFileName = ".3c96f3db6d304e1b984476dc380e5aea";

		ISerializer _serializer = new NewtonsoftJsonSerializer();

		IDictionary<string, DateTime> _modifiedOnDict = new Dictionary<string, DateTime>();

		readonly SemaphoreSlim _saveModifiedOnDictLock = new SemaphoreSlim(1);

		Int32 _saveModifiedOnDictTaskRunning = 0;

		string FileNameForModifiedOn(string filename)
		{
			string path = _current.Path.Substring(Math.Min(_current.Path.Length, _root.Path.Length + 1));
			return Path.Combine(path, filename);
		}

		void ModifyModifiedOnDict(Action action)
		{
			_saveModifiedOnDictLock.Wait();
			try
			{
				action();
			}
			finally
			{
				_saveModifiedOnDictLock.Release();
			}
		}

		async Task LoadModifiedOnDictAsync()
		{
			ModifyModifiedOnDict(_modifiedOnDict.Clear);

			IStorageItem storageFile = await _root.TryGetItemAsync(_modifiedOnDictFileName);
			if (storageFile != null)
			{
				await _saveModifiedOnDictLock.WaitAsync();
				try
				{
					string d = await FileIO.ReadTextAsync(storageFile as StorageFile);
					d = Obfuscator.DeObfuscate(d);
					_modifiedOnDict = _serializer.Deserialize<IDictionary<string, DateTime>>(d);
				}
				finally
				{
					_saveModifiedOnDictLock.Release();
				}
			}
		}

		async Task SaveModifiedOnDictTask()
		{
			DateTime sdt = DateTime.Now;

			await _saveModifiedOnDictLock.WaitAsync();
			try
			{
				IStorageFile storageFile = await _root.CreateFileAsync(_modifiedOnDictFileName, CreationCollisionOption.ReplaceExisting);
				string d = _serializer.Serialize(_modifiedOnDict);
				d = Obfuscator.Obfuscate(d);
				await FileIO.WriteTextAsync(storageFile, d);
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
			}
			finally
			{
				Interlocked.Exchange(ref _saveModifiedOnDictTaskRunning, 0);
				_saveModifiedOnDictLock.Release();

				Log.D($"duration: {(DateTime.Now - sdt).Milliseconds}", this);
			}
		}

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

		async Task SaveModifiedOnDictAsync()
		{
			if (Interlocked.Exchange(ref _saveModifiedOnDictTaskRunning, 1) == 1)
				return;

			Task.Run(async () =>
			{
				await Task.Delay(1000);
				await SaveModifiedOnDictTask();
			});
		}

#pragma warning restore CS4014

		class State
		{
			public StorageFolder savedRoot;
			public StorageFolder savedCurrent;
		};

		Stack<State> _stateStack = new Stack<State>();

		public Task SaveStateAsync()
		{
			_stateStack.Push(new State
			{
				savedRoot = _root,
				savedCurrent = _current,
			});

			return Task.FromResult(true);
		}

		public Task RestoreStateAsync()
		{
			if (_stateStack.Count > 0)
			{
				State state = _stateStack.Pop();
				_root = state.savedRoot;
				_current = state.savedCurrent;
			}

			return Task.FromResult(true);
		}

		public async Task SetCurrentDirectoryAsync(string dirname)
		{
			if (dirname == null || dirname == "")
			{
				_current = _root;
			}
			else if (dirname == "..")
			{
				if (_current != _root)
					_current = await _current.GetParentAsync();
			}
			else
			{
				_current = await _current.GetFolderAsync(dirname);
			}
		}


		public async Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "")
		{
			IEnumerable<string> dirnames =
				from storageDir in await _current.GetFoldersAsync()
				where Regex.IsMatch(storageDir.Name, pattern)
				select storageDir.Name;
			return dirnames;
		}

		public async Task<bool> DirectoryExistsAsync(string dirname)
		{
			return await _current.TryGetItemAsync(dirname) != null;
		}

		public async Task CreateDirectoryAsync(string dirname)
		{
			if (!string.IsNullOrEmpty(dirname))
				_current = await _current.CreateFolderAsync(dirname, CreationCollisionOption.OpenIfExists);
		}

		public async Task DeleteDirectoryAsync(string dirname)
		{
			try
			{
				IStorageFolder storageDir = await _current.GetFolderAsync(dirname);
				await storageDir.DeleteAsync();
			}
			catch (FileNotFoundException) { }
		}


		public async Task<IEnumerable<string>> GetFilesAsync(string pattern = "")
		{
			IEnumerable<string> filenames =
				from storageFile in await _current.GetFilesAsync()
				where Regex.IsMatch(storageFile.Name, pattern)
				select storageFile.Name;
			return filenames;
		}

		public async Task<bool> FileExistsAsync(string filename)
		{
			return await _current.TryGetItemAsync(filename) != null;
		}

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

		public async Task DeleteFileAsync(string filename)
		{
			try
			{
				IStorageFile storageFile = await _current.GetFileAsync(filename);
				await storageFile.DeleteAsync();

				ModifyModifiedOnDict(() => _modifiedOnDict.Remove(FileNameForModifiedOn(filename)));
				SaveModifiedOnDictAsync();
			}
			catch (FileNotFoundException) { }
		}

		public async Task SetFileModifiedOnAsync(string filename, DateTime modifiedOn)
		{
			ModifyModifiedOnDict(() => _modifiedOnDict[FileNameForModifiedOn(filename)] = modifiedOn.ToUniversalTime());
			SaveModifiedOnDictAsync();
		}

#pragma warning restore CS4014

		public async Task<DateTime> GetFileModifiedOnAsync(string filename)
		{
			try
			{
				if (_modifiedOnDict.TryGetValue(FileNameForModifiedOn(filename), out DateTime modifiedOn))
					return modifiedOn;

				IStorageFile storageFile = await _current.GetFileAsync(filename);
				BasicProperties props = await storageFile.GetBasicPropertiesAsync();
				return props.DateModified.DateTime.ToUniversalTime();
			}
			catch (FileNotFoundException) { }
			return DateTime.MinValue;
		}

		public async Task WriteTextAsync(string filename, string text)
		{
			IStorageFile storageFile = await _current.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
			await FileIO.WriteTextAsync(storageFile, text);
		}

		public async Task<string> ReadTextAsync(string filename)
		{
			IStorageFile storageFile = await _current.GetFileAsync(filename);
			return await FileIO.ReadTextAsync(storageFile);
		}
	}
}

#endif
