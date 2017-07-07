using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pbXNet
{
	public class FileSystemInDatabase : IFileSystem, IDisposable
	{
		public FileSystemType Type { get; private set; }

		/// <summary>
		/// The file system identifier, and the table name in the database.
		/// </summary>
		public string Id { get; }

		public string Name => Db?.Name;

		public string RootPath { get; protected set; }

		public string CurrentPath { get; protected set; }

		protected Stack<string> VisitedPaths = new Stack<string>();

		protected class Entry
		{
			public string Path { get; set; }
			public string Name { get; set; }
			public bool IsDirectory { get; set; }
			public string Data { get; set; }
			public DateTime CreatedOn { get; set; }
			public DateTime ModifiedOn { get; set; }
		}

		protected IDatabase Db;
		protected ITable<Entry> Entries;

		Entry _pk = new Entry();

		protected FileSystemInDatabase(string id, IDatabase db)
		{
			Id = id ?? throw new ArgumentNullException(nameof(id));
			Db = db ?? throw new ArgumentNullException(nameof(db));
			RootPath = CurrentPath = "/";
		}

		public static async Task<IFileSystem> NewAsync(string id, IDatabase db)
		{
			if (id == null)
				throw new ArgumentNullException(nameof(id));
			if (db == null)
				throw new ArgumentNullException(nameof(db));

			var fs = new FileSystemInDatabase(id, db);
			await fs.InitializeAsync();
			return fs;
		}

		public virtual void Initialize()
		{
			InitializeAsync().GetAwaiter().GetResult();
		}

		public virtual async Task InitializeAsync()
		{
			Entries = await Db.CreateTableAsync<Entry>(Id).ConfigureAwait(false);

			await Entries.CreatePrimaryKeyAsync(e => e.Path, e => e.Name).ConfigureAwait(false);
			await Entries.CreateIndexAsync(false, e => e.Path).ConfigureAwait(false);
		}

		public virtual void Dispose()
		{
			VisitedPaths.Clear();
			RootPath = CurrentPath = null;
		}

		public virtual async Task<IFileSystem> CloneAsync()
		{
			FileSystemInDatabase fs = new FileSystemInDatabase(Id, Db)
			{
				RootPath = this.RootPath,
				CurrentPath = this.CurrentPath,
				VisitedPaths = new Stack<string>(VisitedPaths.AsEnumerable()),
			};

			await InitializeAsync();

			return fs;
		}

		protected DeviceFileSystem.State States = new DeviceFileSystem.State();

		public virtual Task SaveStateAsync()
		{
			States.Save(RootPath, CurrentPath, VisitedPaths);
			return Task.FromResult(true);
		}

		public virtual Task RestoreStateAsync()
		{
			string rootPath = "", currentPath = "";
			if (States.Restore(ref rootPath, ref currentPath, ref VisitedPaths))
			{
				RootPath = rootPath;
				CurrentPath = currentPath;
			}
			return Task.FromResult(true);
		}

		public virtual async Task SetCurrentDirectoryAsync(string dirname)
		{
			if (string.IsNullOrEmpty(dirname))
			{
				CurrentPath = RootPath;
				VisitedPaths.Clear();
			}
			else if (dirname == "..")
			{
				CurrentPath = VisitedPaths.Count > 0 ? VisitedPaths.Pop() : RootPath;
			}
			else
			{
				if (!await DirectoryExistsAsync(dirname).ConfigureAwait(false))
					throw new DirectoryNotFoundException(T.Localized("FS_DirNotFound", CurrentPath, dirname));

				VisitedPaths.Push(CurrentPath);
				CurrentPath = GetFilePath(dirname);
			}
		}

		public virtual async Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "")
		{
			bool emptyPattern = string.IsNullOrWhiteSpace(pattern);
			return
				(await Entries.Rows
					.Where(e => e.Path == CurrentPath && e.IsDirectory)
					.Where(e => emptyPattern || Regex.IsMatch(e.Name, pattern))
					.PrepareAsync())
					.Select((e) => e.Name);
		}

		public virtual async Task<bool> DirectoryExistsAsync(string dirname)
		{
			if (string.IsNullOrEmpty(dirname))
				return false;

			_pk.Path = CurrentPath;
			_pk.Name = dirname;
			return await Entries.FindAsync(_pk).ConfigureAwait(false) != null;
		}

		public virtual async Task CreateDirectoryAsync(string dirname)
		{
			if (string.IsNullOrEmpty(dirname))
				throw new ArgumentNullException(nameof(dirname));

			if (!await DirectoryExistsAsync(dirname).ConfigureAwait(false))
			{
				await Entries.InsertAsync(
					new Entry
					{
						Path = CurrentPath,
						Name = dirname,
						IsDirectory = true,
						CreatedOn = DateTime.UtcNow,
						ModifiedOn = DateTime.UtcNow,
					})
					.ConfigureAwait(false);
			}

			VisitedPaths.Push(CurrentPath);
			CurrentPath = GetFilePath(dirname);
		}

		public virtual async Task DeleteDirectoryAsync(string dirname)
		{
			if (string.IsNullOrEmpty(dirname))
				throw new ArgumentNullException(nameof(dirname));

			if (!await DirectoryExistsAsync(dirname).ConfigureAwait(false))
				return;

			string dirpath = GetFilePath(dirname);
			var q = Entries.Rows
				.Where(e => e.Path == dirpath);
			if (await q.AnyAsync().ConfigureAwait(false))
				throw new IOException(T.Localized("FS_DirNotEmpty", CurrentPath, dirname));

			_pk.Path = CurrentPath;
			_pk.Name = dirname;
			await Entries.DeleteAsync(_pk).ConfigureAwait(false);
		}

		public virtual async Task<IEnumerable<string>> GetFilesAsync(string pattern = "")
		{
			bool emptyPattern = string.IsNullOrWhiteSpace(pattern);
			return 
				(await Entries.Rows
					.Where(e => e.Path == CurrentPath && !e.IsDirectory)
					.Where(e => emptyPattern || Regex.IsMatch(e.Name, pattern))
					.PrepareAsync())
					.Select((e) => e.Name);
		}

		protected virtual async Task<Entry> GetFsEntryAsync(string filename)
		{
			_pk.Path = CurrentPath;
			_pk.Name = filename;
			return await Entries.FindAsync(_pk).ConfigureAwait(false);
		}

		public virtual async Task<bool> FileExistsAsync(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return false;

			return await GetFsEntryAsync(filename).ConfigureAwait(false) != null;
		}

		public virtual async Task DeleteFileAsync(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException(nameof(filename));

			if (!await FileExistsAsync(filename).ConfigureAwait(false))
				return;

			_pk.Path = CurrentPath;
			_pk.Name = filename;
			await Entries.DeleteAsync(_pk).ConfigureAwait(false);
		}

		public virtual async Task SetFileModifiedOnAsync(string filename, DateTime modifiedOn)
		{
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException(nameof(filename));

			Entry e = await GetFsEntryAsync(filename).ConfigureAwait(false);
			if (e == null)
				throw new FileNotFoundException(T.Localized("FS_FileNotFound", CurrentPath, filename));

			e.ModifiedOn = modifiedOn.ToUniversalTime();
			await Entries.UpdateAsync(e).ConfigureAwait(false);
		}

		public virtual async Task<DateTime> GetFileModifiedOnAsync(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException(nameof(filename));

			Entry e = await GetFsEntryAsync(filename).ConfigureAwait(false);
			if (e == null)
				throw new FileNotFoundException(T.Localized("FS_FileNotFound", CurrentPath, filename));

			return e.ModifiedOn;
		}

		public virtual async Task WriteTextAsync(string filename, string text)
		{
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException(nameof(filename));

			await Entries.InsertAsync(
				new Entry
				{
					Path = CurrentPath,
					Name = filename,
					IsDirectory = false,
					Data = text,
					CreatedOn = DateTime.UtcNow,
					ModifiedOn = DateTime.UtcNow,
				})
				.ConfigureAwait(false);
		}

		public virtual async Task<string> ReadTextAsync(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException(nameof(filename));

			Entry e = await GetFsEntryAsync(filename).ConfigureAwait(false);
			if (e == null)
				throw new FileNotFoundException(T.Localized("FS_FileNotFound", CurrentPath, filename));

			return e.Data;
		}

		#region Tools

		protected virtual string GetFilePath(string filename)
		{
			return Path.Combine(CurrentPath, filename).Replace(Path.DirectorySeparatorChar, '/');
		}

		#endregion
	}
}
