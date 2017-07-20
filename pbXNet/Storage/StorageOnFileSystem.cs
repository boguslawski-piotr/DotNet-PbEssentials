using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace pbXNet
{
	public class StorageOnFileSystem<T> : Storage<T>, ISearchableStorage<T>
	{
		public override StorageType Type => _fs.Type == FileSystemType.Local ? StorageType.LocalIO : StorageType.RemoteIO;

		public override string Name => _fs.Name;

		protected IFileSystem _fs;

		/// Id will be used as directory name in the root of file system.
		/// In this directory all data will be stored.
		public StorageOnFileSystem(string id, IFileSystem fs, ISerializer serializer = null)
			: base(id, serializer)
		{
			Check.Null(fs, nameof(fs));

			_fs = fs;
		}

		public static async Task<StorageOnFileSystem<T>> NewAsync(string id, IFileSystem fs, ISerializer serializer = null)
		{
			StorageOnFileSystem<T> o = new StorageOnFileSystem<T>(id, fs, serializer);
			await o.InitializeAsync().ConfigureAwait(false);
			return o;
		}

		public override async Task InitializeAsync()
		{
			await _fs.SetCurrentDirectoryAsync(null).ConfigureAwait(false);
		}

		protected virtual async Task<IFileSystem> GetFsAsync()
		{
			IFileSystem fs = await _fs.CloneAsync();

			await fs.CreateDirectoryAsync(Id).ConfigureAwait(false);

			return fs;
		}

		public override async Task StoreAsync(string thingId, T data, DateTime modifiedOn)
		{
			Check.Empty(thingId, nameof(thingId));
			Check.Null(data, nameof(data));

			IFileSystem fs = await GetFsAsync().ConfigureAwait(false);

			await fs.WriteTextAsync(thingId, Serializer.Serialize<T>(data)).ConfigureAwait(false);

			await fs.SetFileModifiedOnAsync(thingId, modifiedOn).ConfigureAwait(false);
		}

		public override async Task<bool> ExistsAsync(string thingId)
		{
			Check.Empty(thingId, nameof(thingId));

			// We do not use GetFsAsync() in order to delay directory creation as much as possible.

			IFileSystem fs = await _fs.CloneAsync();

			await fs.SetCurrentDirectoryAsync(null).ConfigureAwait(false);
			if (!await fs.DirectoryExistsAsync(Id).ConfigureAwait(false))
				return false;

			await fs.SetCurrentDirectoryAsync(Id).ConfigureAwait(false);
			return await fs.FileExistsAsync(thingId).ConfigureAwait(false);
		}

		public override async Task<DateTime> GetModifiedOnAsync(string thingId)
		{
			Check.Empty(thingId, nameof(thingId));

			IFileSystem fs = await GetFsAsync().ConfigureAwait(false);

			if(!await fs.FileExistsAsync(thingId).ConfigureAwait(false))
				throw new StorageThingNotFoundException(thingId);

			return await fs.GetFileModifiedOnAsync(thingId).ConfigureAwait(false);
		}

		public override async Task<T> GetACopyAsync(string thingId)
		{
			Check.Empty(thingId, nameof(thingId));

			IFileSystem fs = await GetFsAsync().ConfigureAwait(false);

			if (!await fs.FileExistsAsync(thingId).ConfigureAwait(false))
				throw new StorageThingNotFoundException(thingId);

			return Serializer.Deserialize<T>(await fs.ReadTextAsync(thingId).ConfigureAwait(false));
		}

		public override async Task DiscardAsync(string thingId)
		{
			Check.Empty(thingId, nameof(thingId));

			IFileSystem fs = await GetFsAsync().ConfigureAwait(false);
			await fs.DeleteFileAsync(thingId).ConfigureAwait(false);
		}

		public virtual async Task<IEnumerable<string>> FindIdsAsync(string pattern)
		{
			// We do not use GetFsAsync() in order to delay directory creation as much as possible.

			IFileSystem fs = await _fs.CloneAsync();

			await fs.SetCurrentDirectoryAsync(null).ConfigureAwait(false);
			if (!await fs.DirectoryExistsAsync(Id).ConfigureAwait(false))
				return Enumerable.Empty<string>();

			await fs.SetCurrentDirectoryAsync(Id).ConfigureAwait(false);
			return await fs.GetFilesAsync(pattern).ConfigureAwait(false);
		}
	}
}
