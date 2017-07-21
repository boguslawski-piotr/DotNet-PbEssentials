using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using pbXNet.Database;

namespace pbXNet
{
	public class FileSystemInDatabase : IFileSystem, IDisposable
	{
		public FileSystemType Type => _db?.ConnectionType == ConnectionType.Local ? FileSystemType.Local : FileSystemType.Remote;

		/// <summary>
		/// The file system identifier, and the table name in the database.
		/// </summary>
		public string Id { get; }

		public string Name => _db?.Name;

		public string RootPath { get; protected set; }

		public string CurrentPath { get; protected set; }

		protected Stack<string> _visitedPaths = new Stack<string>();

		protected class Entry
		{
			[PrimaryKey]
			[Length(2048)]
			[Index("Path")]
			public string Path;

			[PrimaryKey]
			[Length(256)]
			public string Name;

			[NotNull]
			public bool IsDirectory;

			[Length(int.MaxValue)]
			public string Data;

			[NotNull]
			public long CreatedOn;

			[NotNull]
			public long ModifiedOn;
		}

		protected IDatabase _db;

		bool _disposeDb;

		protected ITable<Entry> _table;

		Entry _pk = new Entry();

		protected FileSystemInDatabase(string id, IDatabase db, bool disposeDb = false)
		{
			Id = id;
			_db = db;
			_disposeDb = disposeDb;

			RootPath = CurrentPath = "/";
		}

		public static async Task<IFileSystem> NewAsync(string id, IDatabase db, bool disposeDb = false)
		{
			Check.Null(id, nameof(id));
			Check.Null(db, nameof(db));

			var fs = new FileSystemInDatabase(id, db, disposeDb);
			await fs.InitializeAsync();
			return fs;
		}

		public virtual void Initialize()
		{
			InitializeAsync().GetAwaiter().GetResult();
		}

		public virtual async Task InitializeAsync()
		{
			_table = await _db.TableAsync<Entry>(Id).ConfigureAwait(false);
		}

		public virtual async Task<IFileSystem> CloneAsync()
		{
			FileSystemInDatabase fs = new FileSystemInDatabase(Id, _db, false)
			{
				RootPath = this.RootPath,
				CurrentPath = this.CurrentPath,
				_visitedPaths = new Stack<string>(_visitedPaths.AsEnumerable()),
			};

			fs._table = await _db.TableAsync<Entry>(Id, _table == null).ConfigureAwait(false);

			return fs;
		}

		public virtual void Dispose()
		{
			if (_disposeDb)
				_db?.Dispose();

			_db = null;
			_table = null;

			_visitedPaths.Clear();
			RootPath = CurrentPath = null;
		}

		protected DeviceFileSystem.State States = new DeviceFileSystem.State();

		public virtual Task SaveStateAsync()
		{
			States.Save(RootPath, CurrentPath, _visitedPaths);
			return Task.FromResult(true);
		}

		public virtual Task RestoreStateAsync()
		{
			string rootPath = "", currentPath = "";
			if (States.Restore(ref rootPath, ref currentPath, ref _visitedPaths))
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
				_visitedPaths.Clear();
			}
			else if (dirname == "..")
			{
				CurrentPath = _visitedPaths.Count > 0 ? _visitedPaths.Pop() : RootPath;
			}
			else
			{
				if (!await DirectoryExistsAsync(dirname).ConfigureAwait(false))
					throw new DirectoryNotFoundException(Localized.T("FS_DirNotFound", CurrentPath, dirname));

				_visitedPaths.Push(CurrentPath);
				CurrentPath = GetFilePath(dirname);
			}
		}

		public virtual async Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "")
		{
			bool emptyPattern = string.IsNullOrWhiteSpace(pattern);
			using (var rows = _table.Rows)
			using (var q = await rows
				.Where(e => e.Path == CurrentPath && e.IsDirectory == true)
				.Where(e => emptyPattern || Regex.IsMatch(e.Name, pattern))
				.ResultAsync()
			)
			{
				return q
					.Select((e) => e.Name)
					.ToList();
			}
		}

		public virtual async Task<bool> DirectoryExistsAsync(string dirname)
		{
			if (string.IsNullOrEmpty(dirname))
				return false;

			_pk.Path = CurrentPath;
			_pk.Name = dirname;
			return await _table.FindAsync(_pk).ConfigureAwait(false) != null;
		}

		public virtual async Task CreateDirectoryAsync(string dirname)
		{
			Check.Empty(dirname, nameof(dirname));

			if (!await DirectoryExistsAsync(dirname).ConfigureAwait(false))
			{
				await _table.InsertOrUpdateAsync(
					new Entry
					{
						Path = CurrentPath,
						Name = dirname,
						IsDirectory = true,
						CreatedOn = DateTime.UtcNow.ToBinary(),
						ModifiedOn = DateTime.UtcNow.ToBinary(),
					})
					.ConfigureAwait(false);
			}

			_visitedPaths.Push(CurrentPath);
			CurrentPath = GetFilePath(dirname);
		}

		public virtual async Task DeleteDirectoryAsync(string dirname)
		{
			Check.Empty(dirname, nameof(dirname));

			if (!await DirectoryExistsAsync(dirname).ConfigureAwait(false))
				return;

			string dirpath = GetFilePath(dirname);
			var q = _table.Rows
				.Where(e => e.Path == dirpath);
			if (await q.AnyAsync().ConfigureAwait(false))
				throw new IOException(Localized.T("FS_DirNotEmpty", CurrentPath, dirname));

			_pk.Path = CurrentPath;
			_pk.Name = dirname;
			await _table.DeleteAsync(_pk).ConfigureAwait(false);
		}

		public virtual async Task<IEnumerable<string>> GetFilesAsync(string pattern = "")
		{
			bool emptyPattern = string.IsNullOrWhiteSpace(pattern);
			using (var rows = _table.Rows)
			using (var q = await rows
				.Where(e => e.Path == CurrentPath && e.IsDirectory == false)
				.Where(e => emptyPattern || Regex.IsMatch(e.Name, pattern))
				.ResultAsync()
			)
			{
				return q
					.Select((e) => e.Name)
					.ToList();
			}
		}

		protected virtual async Task<Entry> GetFsEntryAsync(string filename)
		{
			_pk.Path = CurrentPath;
			_pk.Name = filename;
			return await _table.FindAsync(_pk).ConfigureAwait(false);
		}

		public virtual async Task<bool> FileExistsAsync(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return false;

			return await GetFsEntryAsync(filename).ConfigureAwait(false) != null;
		}

		public virtual async Task DeleteFileAsync(string filename)
		{
			Check.Empty(filename, nameof(filename));

			if (!await FileExistsAsync(filename).ConfigureAwait(false))
				return;

			_pk.Path = CurrentPath;
			_pk.Name = filename;
			await _table.DeleteAsync(_pk).ConfigureAwait(false);
		}

		public virtual async Task SetFileModifiedOnAsync(string filename, DateTime modifiedOn)
		{
			Check.Empty(filename, nameof(filename));

			Entry e = await GetFsEntryAsync(filename).ConfigureAwait(false);
			if (e == null)
				throw new FileNotFoundException(Localized.T("FS_FileNotFound", CurrentPath, filename));

			e.ModifiedOn = modifiedOn.ToUniversalTime().ToBinary();

			await _table.UpdateAsync(e).ConfigureAwait(false);
		}

		public virtual async Task<DateTime> GetFileModifiedOnAsync(string filename)
		{
			Check.Empty(filename, nameof(filename));

			Entry e = await GetFsEntryAsync(filename).ConfigureAwait(false);
			if (e == null)
				throw new FileNotFoundException(Localized.T("FS_FileNotFound", CurrentPath, filename));

			return DateTime.FromBinary(e.ModifiedOn);
		}

		public virtual async Task WriteTextAsync(string filename, string text)
		{
			Check.Empty(filename, nameof(filename));

			await _table.InsertOrUpdateAsync(
				new Entry
				{
					Path = CurrentPath,
					Name = filename,
					IsDirectory = false,
					Data = text,
					CreatedOn = DateTime.UtcNow.ToBinary(),
					ModifiedOn = DateTime.UtcNow.ToBinary(),
				})
				.ConfigureAwait(false);
		}

		public virtual async Task<string> ReadTextAsync(string filename)
		{
			Check.Empty(filename, nameof(filename));

			Entry e = await GetFsEntryAsync(filename).ConfigureAwait(false);
			if (e == null)
				throw new FileNotFoundException(Localized.T("FS_FileNotFound", CurrentPath, filename));

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
