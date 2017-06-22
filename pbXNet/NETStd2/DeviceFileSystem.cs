#if !WINDOWS_UWP

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pbXNet
{
	public partial class DeviceFileSystem : IFileSystem, IDisposable
	{
		public static readonly IEnumerable<DeviceFileSystemRoot> AvailableRootsForEndUser = new List<DeviceFileSystemRoot>() {
			DeviceFileSystemRoot.Local,
#if DEBUG
			//DeviceFileSystemRoot.Config, // only for testing
#endif
        };

		string _root;
		string _current;
		Stack<string> _previous = new Stack<string>();

		public string RootPath => _root;
		public string CurrentPath => _current;

		protected virtual void Initialize(string userDefinedRootPath)
		{
			switch (Root)
			{
				case DeviceFileSystemRoot.UserDefined:
					if (userDefinedRootPath == null)
						throw new ArgumentNullException(nameof(userDefinedRootPath));

					if (userDefinedRootPath.StartsWith("~", StringComparison.Ordinal))
					{
#if NETSTANDARD1_6 || __MACOS__
						string home = Environment.GetEnvironmentVariable("HOME");
#else
						string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#endif
						userDefinedRootPath = Path.Combine(home, userDefinedRootPath.Replace("~/", "").Replace("~", ""));
					}

					if (!Directory.Exists(userDefinedRootPath))
					{
						Exception ex = new DirectoryNotFoundException($"Directory {userDefinedRootPath} not found.");
						Log.E(ex.Message, this);
						throw ex;
					}

					_root = userDefinedRootPath;
					break;

				case DeviceFileSystemRoot.RoamingConfig:
				case DeviceFileSystemRoot.LocalConfig:
#if NETSTANDARD1_6
					_root = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".config");
					Directory.CreateDirectory(_root);
#else
					_root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif
					break;

				case DeviceFileSystemRoot.Roaming:
				case DeviceFileSystemRoot.Local:
				default:
#if NETSTANDARD1_6 || __MACOS__
					_root = Environment.GetEnvironmentVariable("HOME");
					_root = Path.Combine(_root, "Documents");
					if (!Directory.Exists(_root))
						Directory.CreateDirectory(_root);
#else
					_root = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif
					break;
			}

			_current = _root;
			_previous.Clear();
		}

		public virtual void Dispose()
		{
			_previous.Clear();
			_root = _current = null;
		}

		public virtual Task<IFileSystem> MakeCopyAsync()
		{
			DeviceFileSystem fs = new DeviceFileSystem(this.Root, _root)
			{
				_root = this._root,
				_current = this._current,
				_previous = new Stack<string>(_previous.AsEnumerable()),
			};
			return Task.FromResult<IFileSystem>(fs);
		}

		class State
		{
			public string savedRoot;
			public string savedCurrent;
			public Stack<string> savedPrevious;
		}

		Stack<State> _stateStack = new Stack<State>();

		public virtual Task SaveStateAsync()
		{
			_stateStack.Push(new State
			{
				savedRoot = _root,
				savedCurrent = _current,
				savedPrevious = new Stack<string>(_previous.AsEnumerable()),
			});

			return Task.FromResult(true);
		}

		public virtual Task RestoreStateAsync()
		{
			if (_stateStack.Count > 0)
			{
				State state = _stateStack.Pop();
				_root = state.savedRoot;
				_current = state.savedCurrent;
				_previous = state.savedPrevious;
			}

			return Task.FromResult(true);
		}

		public virtual Task SetCurrentDirectoryAsync(string dirname)
		{
			if (string.IsNullOrEmpty(dirname))
			{
				_current = _root;
				_previous.Clear();
			}
			else if (dirname == "..")
			{
				_current = _previous.Count > 0 ? _previous.Pop() : _root;
			}
			else
			{
				_previous.Push(_current);
				_current = Path.Combine(_current, dirname);
			}
			return Task.FromResult(true);
		}

		public virtual Task<bool> DirectoryExistsAsync(string dirname)
		{
			string dirpath = GetFilePath(dirname);
			bool exists = Directory.Exists(dirpath);
			return Task.FromResult(exists);
		}

		public virtual Task<bool> FileExistsAsync(string filename)
		{
			string filepath = GetFilePath(filename);
			bool exists = File.Exists(filepath);
			return Task.FromResult(exists);
		}

		public virtual Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "")
		{
			IEnumerable<string> dirnames =
				from dirpath in Directory.EnumerateDirectories(_current)
				let dirname = Path.GetFileName(dirpath)
				where Regex.IsMatch(dirname, pattern)
				select dirname;
			return Task.FromResult(dirnames);
		}

		public virtual Task<IEnumerable<string>> GetFilesAsync(string pattern = "")
		{
			IEnumerable<string> filenames =
				from filepath in Directory.EnumerateFiles(_current)
				let filename = Path.GetFileName(filepath)
				where Regex.IsMatch(filename, pattern)
				select filename;
			return Task.FromResult(filenames);
		}

		public virtual Task CreateDirectoryAsync(string dirname)
		{
			if (!string.IsNullOrEmpty(dirname))
			{
				string dirpath = GetFilePath(dirname);
				DirectoryInfo dir = Directory.CreateDirectory(GetFilePath(dirpath));
				_previous.Push(_current);
				_current = dirpath;
			}
			return Task.FromResult(true);
		}

		public virtual Task DeleteDirectoryAsync(string dirname)
		{
			if (!string.IsNullOrEmpty(dirname))
				Directory.Delete(GetFilePath(dirname));
			return Task.FromResult(true);
		}

		public virtual Task DeleteFileAsync(string filename)
		{
			if (!string.IsNullOrEmpty(filename))
				File.Delete(GetFilePath(filename));
			return Task.FromResult(true);
		}

		public Task SetFileModifiedOnAsync(string filename, DateTime modifiedOn)
		{
			File.SetLastWriteTimeUtc(GetFilePath(filename), modifiedOn.ToUniversalTime());
			return Task.FromResult(true);
		}

		public Task<DateTime> GetFileModifiedOnAsync(string filename)
		{
			DateTime modifiedOn = File.GetLastWriteTimeUtc(GetFilePath(filename));
			return Task.FromResult(modifiedOn);
		}

		public virtual async Task WriteTextAsync(string filename, string text)
		{
			string filepath = GetFilePath(filename);
			using (StreamWriter writer = File.CreateText(filepath))
			{
				await writer.WriteAsync(text).ConfigureAwait(false);
			}
		}

		public virtual async Task<string> ReadTextAsync(string filename)
		{
			string filepath = GetFilePath(filename);
			using (StreamReader reader = File.OpenText(filepath))
			{
				return await reader.ReadToEndAsync().ConfigureAwait(false);
			}
		}

		protected string GetFilePath(string filename)
		{
			return Path.Combine(_current, filename);
		}
	}
}

#endif