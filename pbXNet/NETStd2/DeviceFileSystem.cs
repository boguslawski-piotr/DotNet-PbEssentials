//#if !WINDOWS_UWP

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
		};

		public string RootPath { get; protected set; }

		public string CurrentPath { get; protected set; }

		Stack<string> _visitedPaths = new Stack<string>();

#if NETSTANDARD1_6 || __MACOS__
		string SpecialFolderUserProfile
		{
			get {
				string _HomeDir = Environment.GetEnvironmentVariable("HOME");
				if (string.IsNullOrWhiteSpace(_HomeDir))
					_HomeDir = Environment.GetEnvironmentVariable("USERPROFILE");
				if (string.IsNullOrWhiteSpace(_HomeDir))
					_HomeDir = Path.Combine(Environment.GetEnvironmentVariable("HOMEDRIVE"), Environment.GetEnvironmentVariable("HOMEPATH"));
				if (string.IsNullOrWhiteSpace(_HomeDir))
				{
					Exception ex = new DirectoryNotFoundException("Can not find home directory.");
					Log.E(ex.Message, this);
					throw ex;
				}

				return _HomeDir;
			}
		}
#endif

		public virtual void Initialize()
		{
			switch (Root)
			{
				case DeviceFileSystemRoot.UserDefined:
					if (string.IsNullOrWhiteSpace(_userDefinedRootPath))
						throw new ArgumentNullException(nameof(_userDefinedRootPath));

					if (_userDefinedRootPath.StartsWith("~", StringComparison.Ordinal))
					{
#if NETSTANDARD1_6 || __MACOS__
						string home = SpecialFolderUserProfile;
#else
						string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#endif
						_userDefinedRootPath = Path.Combine(home, _userDefinedRootPath.Replace("~/", "").Replace("~", ""));
					}

					RootPath = _userDefinedRootPath;
					break;

				case DeviceFileSystemRoot.RoamingConfig:
				case DeviceFileSystemRoot.LocalConfig:
#if NETSTANDARD1_6
					RootPath = Path.Combine(SpecialFolderUserProfile, ".config");
#else
					RootPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif
					break;

				case DeviceFileSystemRoot.Roaming:
				case DeviceFileSystemRoot.Local:
				default:
#if NETSTANDARD1_6 || __MACOS__
					RootPath = Path.Combine(SpecialFolderUserProfile, "Documents");
#else
					RootPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif
					break;
			}

			if (!Directory.Exists(RootPath))
			{
				Directory.CreateDirectory(RootPath);
				Log.I($"Directory '{RootPath}' has just been created.", this);
			}

			CurrentPath = RootPath;
			_visitedPaths.Clear();
		}

		public virtual void Dispose()
		{
			_visitedPaths.Clear();
			RootPath = CurrentPath = null;
		}

		public virtual Task<IFileSystem> CloneAsync()
		{
			DeviceFileSystem fs = new DeviceFileSystem(this.Root, _userDefinedRootPath)
			{
				RootPath = this.RootPath,
				CurrentPath = this.CurrentPath,
				_visitedPaths = new Stack<string>(_visitedPaths.AsEnumerable()),
			};
			return Task.FromResult<IFileSystem>(fs);
		}

		State _state = new State();

		public virtual Task SaveStateAsync()
		{
			_state.Save(RootPath, CurrentPath, _visitedPaths);
			return Task.FromResult(true);
		}

		public virtual Task RestoreStateAsync()
		{
			string rootPath = "", currentPath = "";
			if (_state.Restore(ref rootPath, ref currentPath, ref _visitedPaths))
			{
				RootPath = rootPath;
				CurrentPath = currentPath;
			}
			return Task.FromResult(true);
		}


		public virtual Task SetCurrentDirectoryAsync(string dirname)
		{
			if (string.IsNullOrEmpty(dirname))
			{
				CurrentPath = RootPath;
				_visitedPaths.Clear();
			}
			else if (dirname == "..")
			{
				CurrentPath = _visitedPaths.Count > 0 ? _visitedPaths.Pop() : RootPath;
			}
			else
			{
				string dirpath = GetFilePath(dirname);
				if (!Directory.Exists(dirpath))
					throw new DirectoryNotFoundException(T.Localized("FS_DirNotFound", CurrentPath, dirname));

				_visitedPaths.Push(CurrentPath);
				CurrentPath = dirpath;
			}
			return Task.FromResult(true);
		}

		public virtual Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "")
		{
			IEnumerable<string> dirnames =
				from dirpath in Directory.EnumerateDirectories(CurrentPath)
				let dirname = Path.GetFileName(dirpath)
				where Regex.IsMatch(dirname, pattern)
				select dirname;
			return Task.FromResult(dirnames);
		}

		public virtual Task<bool> DirectoryExistsAsync(string dirname)
		{
			if (string.IsNullOrEmpty(dirname))
				return Task.FromResult(false);

			string dirpath = GetFilePath(dirname);
			bool exists = Directory.Exists(dirpath);
			return Task.FromResult(exists);
		}

		public virtual Task CreateDirectoryAsync(string dirname)
		{
			if (string.IsNullOrEmpty(dirname))
				throw new ArgumentNullException(nameof(dirname));

			string dirpath = GetFilePath(dirname);
			DirectoryInfo dir = Directory.CreateDirectory(GetFilePath(dirpath));

			_visitedPaths.Push(CurrentPath);
			CurrentPath = dirpath;
			return Task.FromResult(true);
		}

		public virtual Task DeleteDirectoryAsync(string dirname)
		{
			if (string.IsNullOrEmpty(dirname))
				throw new ArgumentNullException(nameof(dirname));

			string dirpath = GetFilePath(dirname);
			if (Directory.Exists(dirpath))
				Directory.Delete(dirpath);

			return Task.FromResult(true);
		}


		public virtual Task<IEnumerable<string>> GetFilesAsync(string pattern = "")
		{
			IEnumerable<string> filenames =
				from filepath in Directory.EnumerateFiles(CurrentPath)
				let filename = Path.GetFileName(filepath)
				where Regex.IsMatch(filename, pattern)
				select filename;
			return Task.FromResult(filenames);
		}

		public virtual Task<bool> FileExistsAsync(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return Task.FromResult(false);

			string filepath = GetFilePath(filename);
			bool exists = File.Exists(filepath);
			return Task.FromResult(exists);
		}

		public virtual Task DeleteFileAsync(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException(nameof(filename));

			string filepath = GetFilePath(filename);
			if (File.Exists(filepath))
				File.Delete(filepath);

			return Task.FromResult(true);
		}

		public virtual Task SetFileModifiedOnAsync(string filename, DateTime modifiedOn)
		{
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException(nameof(filename));

			File.SetLastWriteTimeUtc(GetFilePath(filename), modifiedOn.ToUniversalTime());
			return Task.FromResult(true);
		}

		public virtual Task<DateTime> GetFileModifiedOnAsync(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException(nameof(filename));

			DateTime modifiedOn = File.GetLastWriteTimeUtc(GetFilePath(filename));
			return Task.FromResult(modifiedOn);
		}

		public virtual async Task WriteTextAsync(string filename, string text)
		{
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException(nameof(filename));

			string filepath = GetFilePath(filename);
			using (StreamWriter writer = File.CreateText(filepath))
			{
				await writer.WriteAsync(text).ConfigureAwait(false);
			}
		}

		public virtual async Task<string> ReadTextAsync(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException(nameof(filename));

			string filepath = GetFilePath(filename);
			using (StreamReader reader = File.OpenText(filepath))
			{
				return await reader.ReadToEndAsync().ConfigureAwait(false);
			}
		}

		#region Tools

		protected virtual string GetFilePath(string filename)
		{
			return Path.Combine(CurrentPath, filename);
		}

		#endregion
	}
}

//#endif