//#if !WINDOWS_UWP

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Storage;
#endif

namespace pbXNet
{
	public class DeviceFileSystem : IFileSystem, IDisposable
	{
		public static readonly IEnumerable<RootType> AvailableRootsForEndUser = new List<RootType>() {
			RootType.Local,
#if WINDOWS_UWP
			RootType.Roaming,
#endif
		};

		public FileSystemType Type { get; } = FileSystemType.Local;

		public enum RootType
		{
			Local,
			LocalConfig,
			Roaming,
			RoamingConfig,
			UserDefined,
		}

		public RootType Root { get; }

		public string Id { get; } = Tools.CreateGuid();

		public string Name
		{
			get {
				switch (Root)
				{
					case RootType.Local:
						return T.Localized("DeviceFileSystem.Root.Local");

					case RootType.LocalConfig:
						return T.Localized("DeviceFileSystem.Root.LocalConfig");

					case RootType.Roaming:
						return T.Localized("DeviceFileSystem.Root.Roaming");

					case RootType.RoamingConfig:
						return T.Localized("DeviceFileSystem.Root.RoamingConfig");

					default:
						return RootPath;
				}
			}
		}

		// TODO: dodac Description

		public string RootPath { get; protected set; }

		public string CurrentPath { get; protected set; }

		Stack<string> _visitedPaths = new Stack<string>();

		string _userDefinedRootPath;

		protected DeviceFileSystem(RootType root = RootType.Local, string userDefinedRootPath = null)
		{
			if(root == RootType.UserDefined && string.IsNullOrWhiteSpace(userDefinedRootPath))
				throw new ArgumentNullException(nameof(userDefinedRootPath));

			Root = root;
			_userDefinedRootPath = userDefinedRootPath;
		}

		public static IFileSystem New(RootType root = RootType.Local, string userDefinedRootPath = null)
		{
			IFileSystem fs = new DeviceFileSystem(root, userDefinedRootPath);
			fs.Initialize();
			return fs;
		}

#if NETSTANDARD1_6 || __MACOS__ || WINDOWS_UWP
		string SpecialFolderUserProfile
		{
			get {
#if WINDOWS_UWP
				string home = ApplicationData.Current.LocalFolder.Path;
#else
				string home = Environment.GetEnvironmentVariable("HOME");
				if (string.IsNullOrWhiteSpace(home))
					home = Environment.GetEnvironmentVariable("USERPROFILE");
				if (string.IsNullOrWhiteSpace(home))
					home = Path.Combine(Environment.GetEnvironmentVariable("HOMEDRIVE") ?? "", Environment.GetEnvironmentVariable("HOMEPATH") ?? "");
				if (string.IsNullOrWhiteSpace(home))
				{
					Exception ex = new DirectoryNotFoundException("Can not find home directory.");
					Log.E(ex.Message, this);
					throw ex;
				}
#endif
				return home;
			}
		}
#endif

		public virtual void Initialize()
		{
			switch (Root)
			{
				case RootType.UserDefined:
					if (_userDefinedRootPath.StartsWith("~", StringComparison.Ordinal))
					{
#if NETSTANDARD1_6 || __MACOS__ || WINDOWS_UWP
						string home = SpecialFolderUserProfile;
#else
						string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#endif
						_userDefinedRootPath = Path.Combine(home, _userDefinedRootPath.Replace("~/", "").Replace("~", ""));
					}

					RootPath = _userDefinedRootPath;
					break;

				case RootType.RoamingConfig:
#if WINDOWS_UWP
					RootPath = Path.Combine(ApplicationData.Current.RoamingFolder.Path, ".config");
					break;
#endif
				case RootType.LocalConfig:
#if NETSTANDARD1_6 || WINDOWS_UWP
					RootPath = Path.Combine(SpecialFolderUserProfile, ".config");
#else
					RootPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif
					break;

				case RootType.Roaming:
#if WINDOWS_UWP
					RootPath = ApplicationData.Current.RoamingFolder.Path;
					break;
#endif
				case RootType.Local:
				default:
#if NETSTANDARD1_6 || __MACOS__ || WINDOWS_UWP
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

		public class State
		{
			class Rec
			{
				public string RootPath;
				public string CurrentPath;
				public Stack<string> VisitedPaths;
			}

			Stack<Rec> _stack = new Stack<Rec>();

			public void Save(string rootPath, string currentPath, Stack<string> visitedPaths)
			{
				_stack.Push(new Rec
				{
					RootPath = rootPath,
					CurrentPath = currentPath,
					VisitedPaths = new Stack<string>(visitedPaths.AsEnumerable()),
				});
			}

			public bool Restore(ref string rootPath, ref string currentPath, ref Stack<string> visitedPaths)
			{
				if (_stack.Count > 0)
				{
					Rec entry = _stack.Pop();
					rootPath = entry.RootPath;
					currentPath = entry.CurrentPath;
					visitedPaths = entry.VisitedPaths;
					return true;
				}
				return false;
			}
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
			if(!Directory.Exists(dirpath))
				Directory.CreateDirectory(dirpath);

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