using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace pbXNet
{
	public class StorageOnFileSystem<T> : Storage<T>, ISearchableStorage<T>
	{
		public override StorageType Type => Fs.Type == FileSystemType.Local ? StorageType.LocalIO : StorageType.RemoteIO;

		public override string Name => Fs.Name;

		protected IFileSystem Fs;

		/// Id will be used as directory name in the root of file system.
		/// In this directory all data will be stored.
		public StorageOnFileSystem(string id, IFileSystem fs, ISerializer serializer = null)
			: base(id, serializer)
		{
			Fs = fs;
		}

		public static async Task<StorageOnFileSystem<T>> NewAsync(string id, IFileSystem fs, ISerializer serializer = null)
		{
			StorageOnFileSystem<T> o = new StorageOnFileSystem<T>(id, fs, serializer);
			await o.InitializeAsync();
			return o;
		}

		public override async Task InitializeAsync()
		{
			await Fs.SetCurrentDirectoryAsync(null);
		}

		protected virtual async Task<IFileSystem> GetFsAsync()
		{
			await Fs.SetCurrentDirectoryAsync(null).ConfigureAwait(false);
			await Fs.CreateDirectoryAsync(Id).ConfigureAwait(false);
			return Fs;
		}

		public override async Task StoreAsync(string thingId, T data, DateTime modifiedOn)
		{
			IFileSystem fs = await GetFsAsync().ConfigureAwait(false);

			await fs.WriteTextAsync(thingId, Serializer.Serialize<T>(data)).ConfigureAwait(false);

			await fs.SetFileModifiedOnAsync(thingId, modifiedOn).ConfigureAwait(false);
		}

		public override async Task<bool> ExistsAsync(string thingId)
		{
			// We do not use GetFsAsync() in order to delay directory creation as much as possible.

			await Fs.SetCurrentDirectoryAsync(null).ConfigureAwait(false);
			if (!await Fs.DirectoryExistsAsync(Id).ConfigureAwait(false))
				return false;

			await Fs.SetCurrentDirectoryAsync(Id).ConfigureAwait(false);
			return await Fs.FileExistsAsync(thingId).ConfigureAwait(false);
		}

		public override async Task<DateTime> GetModifiedOnAsync(string thingId)
		{
			if (!await ExistsAsync(thingId).ConfigureAwait(false))
				throw new StorageThingNotFoundException(thingId);

			IFileSystem fs = await GetFsAsync().ConfigureAwait(false);
			return await fs.GetFileModifiedOnAsync(thingId).ConfigureAwait(false);
		}

		public override async Task<T> GetACopyAsync(string thingId)
		{
			if (!await ExistsAsync(thingId).ConfigureAwait(false))
				throw new StorageThingNotFoundException(thingId);

			IFileSystem fs = await GetFsAsync().ConfigureAwait(false);

			return Serializer.Deserialize<T>(await fs.ReadTextAsync(thingId).ConfigureAwait(false));
		}

		public override async Task DiscardAsync(string thingId)
		{
			IFileSystem fs = await GetFsAsync().ConfigureAwait(false);
			await fs.DeleteFileAsync(thingId).ConfigureAwait(false);
		}

		public virtual async Task<IEnumerable<string>> FindIdsAsync(string pattern)
		{
			// We do not use GetFsAsync() in order to delay directory creation as much as possible.

			await Fs.SetCurrentDirectoryAsync(null).ConfigureAwait(false);
			if (!await Fs.DirectoryExistsAsync(Id).ConfigureAwait(false))
				return Enumerable.Empty<string>();

			await Fs.SetCurrentDirectoryAsync(Id).ConfigureAwait(false);
			return await Fs.GetFilesAsync(pattern).ConfigureAwait(false);
		}
	}
}
