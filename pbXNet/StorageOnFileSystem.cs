﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pbXNet
{
	public class StorageOnFileSystem<T> : ISearchableStorage<T> where T : class
	{
		public StorageType Type => Fs.Type == FileSystemType.Local ? StorageType.LocalIO : StorageType.RemoteIO;

		public string Id { get; private set; }

		public virtual string Name => Fs.Name;

		protected IFileSystem Fs;

		protected ISerializer Serializer;

		/// Id will be used as directory name in the root of file system.
		/// In this directory all data will be stored.
		public StorageOnFileSystem(string id, IFileSystem fs, ISerializer serializer)
		{
			Id = id;
			Fs = fs;
			Serializer = serializer;
		}

		public static async Task<StorageOnFileSystem<T>> NewAsync(string id, IFileSystem fs, ISerializer serializer)
		{
			StorageOnFileSystem<T> o = new StorageOnFileSystem<T>(id, fs, serializer);
			await o.InitializeAsync().ConfigureAwait(false);
			return o;
		}

		public virtual async Task InitializeAsync()
		{
			await Fs.SetCurrentDirectoryAsync(null).ConfigureAwait(false);
		}

		protected virtual async Task<IFileSystem> GetFsAsync()
		{
			await Fs.SetCurrentDirectoryAsync(null).ConfigureAwait(false);
			await Fs.CreateDirectoryAsync(Id).ConfigureAwait(false);
			return Fs;
		}

		public virtual async Task StoreAsync(string thingId, T data, DateTime modifiedOn)
		{
			string d;
			if (typeof(T).Equals(typeof(string)))
				d = data.ToString();
			else
				d = Serializer.ToString(data);

			IFileSystem fs = await GetFsAsync().ConfigureAwait(false);
			await fs.WriteTextAsync(thingId, d).ConfigureAwait(false);

			await fs.SetFileModifiedOnAsync(thingId, modifiedOn).ConfigureAwait(false);
		}

		public virtual async Task<bool> ExistsAsync(string thingId)
		{
			// We do not use GetFsAsync() in order to delay directory creation as much as possible.

			await Fs.SetCurrentDirectoryAsync(null).ConfigureAwait(false);
			if (!await Fs.DirectoryExistsAsync(Id).ConfigureAwait(false))
				return false;

			await Fs.SetCurrentDirectoryAsync(Id).ConfigureAwait(false);
			return await Fs.FileExistsAsync(thingId).ConfigureAwait(false);
		}

		public virtual async Task<DateTime> GetModifiedOnAsync(string thingId)
		{
			if (!await ExistsAsync(thingId).ConfigureAwait(false))
				return DateTime.MinValue;

			IFileSystem fs = await GetFsAsync().ConfigureAwait(false);
			return await fs.GetFileModifiedOnAsync(thingId).ConfigureAwait(false);
		}

		public virtual async Task<T> GetACopyAsync(string thingId)
		{
			if (!await ExistsAsync(thingId).ConfigureAwait(false))
				return null;

			IFileSystem fs = await GetFsAsync().ConfigureAwait(false);
			string sd = await fs.ReadTextAsync(thingId).ConfigureAwait(false);

			// Restore object
			object d = sd;
			if (typeof(T).Equals(typeof(string)))
				return (T)d;
			else
				return Serializer.FromString<T>((string)d);
		}

		public virtual async Task<T> RetrieveAsync(string thingId)
		{
			T data = await GetACopyAsync(thingId).ConfigureAwait(false);
			if (data != null)
				await DiscardAsync(thingId).ConfigureAwait(false);
			return data;
		}

		public virtual async Task DiscardAsync(string thingId)
		{
			IFileSystem fs = await GetFsAsync().ConfigureAwait(false);
			await fs.DeleteFileAsync(thingId).ConfigureAwait(false);
		}


		//

		public virtual async Task<IEnumerable<string>> FindIdsAsync(string pattern)
		{
			// We do not use GetFsAsync() in order to delay directory creation as much as possible.

			await Fs.SetCurrentDirectoryAsync(null).ConfigureAwait(false);
			if (!await Fs.DirectoryExistsAsync(Id).ConfigureAwait(false))
				return null;

			await Fs.SetCurrentDirectoryAsync(Id).ConfigureAwait(false);
			return await Fs.GetFilesAsync(pattern).ConfigureAwait(false);
		}
	}
}
