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

		protected Stack<string> _visitedPaths = new Stack<string>();

		protected string _userDefinedRootPath;

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

		public static async Task<IFileSystem> NewAsync(RootType root = RootType.Local, string userDefinedRootPath = null) 
			=> New(root, userDefinedRootPath);

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
				Log.D($"Directory '{RootPath}' has just been created.", this);
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

		protected State States = new State();

		public virtual async Task SaveStateAsync()
		{
			States.Save(RootPath, CurrentPath, _visitedPaths);
		}

		public virtual async Task RestoreStateAsync()
		{
			string rootPath = "", currentPath = "";
			if (States.Restore(ref rootPath, ref currentPath, ref _visitedPaths))
			{
				RootPath = rootPath;
				CurrentPath = currentPath;
			}
		}

		public virtual async Task SetCurrentDirectoryAsync(string dirname)
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
		}

		public virtual async Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "")
		{
			IEnumerable<string> dirnames = Enumerable.Empty<string>();

			await Task.Factory.StartNew(() =>
			{
				bool emptyPattern = string.IsNullOrWhiteSpace(pattern);
				dirnames =
					from dirpath in Directory.EnumerateDirectories(CurrentPath)
					let dirname = Path.GetFileName(dirpath)
					where emptyPattern || Regex.IsMatch(dirname, pattern)
					select dirname;
			})
			.ConfigureAwait(false);

			return dirnames;
		}

		public virtual async Task<bool> DirectoryExistsAsync(string dirname)
		{
			if (string.IsNullOrEmpty(dirname))
				return false;

			string dirpath = GetFilePath(dirname);
			return Directory.Exists(dirpath);
		}

		public virtual async Task CreateDirectoryAsync(string dirname)
		{
			Check.Empty(dirname, nameof(dirname));

			string dirpath = GetFilePath(dirname);
			if(!Directory.Exists(dirpath))
				Directory.CreateDirectory(dirpath);

			_visitedPaths.Push(CurrentPath);
			CurrentPath = dirpath;
		}

		public virtual async Task DeleteDirectoryAsync(string dirname)
		{
			Check.Empty(dirname, nameof(dirname));

			string dirpath = GetFilePath(dirname);
			if (Directory.Exists(dirpath))
				Directory.Delete(dirpath);
		}

		public virtual async Task<IEnumerable<string>> GetFilesAsync(string pattern = "")
		{
			IEnumerable<string> filenames = Enumerable.Empty<string>();

			await Task.Factory.StartNew(() =>
			{
				bool emptyPattern = string.IsNullOrWhiteSpace(pattern);
				filenames =
					from filepath in Directory.EnumerateFiles(CurrentPath)
					let filename = Path.GetFileName(filepath)
					where emptyPattern || Regex.IsMatch(filename, pattern)
					select filename;
			})
			.ConfigureAwait(false);

			return filenames;
		}

		public virtual async Task<bool> FileExistsAsync(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return false;

			string filepath = GetFilePath(filename);
			return File.Exists(filepath);
		}

		public virtual async Task DeleteFileAsync(string filename)
		{
			Check.Empty(filename, nameof(filename));

			string filepath = GetFilePath(filename);
			if (File.Exists(filepath))
				File.Delete(filepath);
		}

		public virtual async Task SetFileModifiedOnAsync(string filename, DateTime modifiedOn)
		{
			Check.Empty(filename, nameof(filename));

			File.SetLastWriteTimeUtc(GetFilePath(filename), modifiedOn.ToUniversalTime());
		}

		public virtual async Task<DateTime> GetFileModifiedOnAsync(string filename)
		{
			Check.Empty(filename, nameof(filename));

			DateTime modifiedOn = File.GetLastWriteTimeUtc(GetFilePath(filename));
			return modifiedOn;
		}

		public virtual async Task WriteTextAsync(string filename, string text)
		{
			Check.Empty(filename, nameof(filename));

			string filepath = GetFilePath(filename);
			using (StreamWriter writer = File.CreateText(filepath))
			{
				await writer.WriteAsync(text).ConfigureAwait(false);
			}
		}

		public virtual async Task<string> ReadTextAsync(string filename)
		{
			Check.Empty(filename, nameof(filename));

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