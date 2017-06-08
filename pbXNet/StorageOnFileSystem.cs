using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pbXNet
{
	public class StorageOnFileSystem<T> : ISearchableStorage<T> where T : class
	{
		public string Id { get; private set; }

		public virtual string Name => Fs.Name;

		protected IFileSystem Fs;

		protected ISerializer Serializer;

		public class SerializerNotSetException : Exception { }

		public StorageOnFileSystem(string id, IFileSystem fs, ISerializer serializer = null)
		{
			Id = id;
			Fs = fs;
			Serializer = serializer;
		}

		public static async Task<StorageOnFileSystem<T>> NewAsync(string id, IFileSystem fs, ISerializer serializer = null)
		{
			StorageOnFileSystem<T> o = new StorageOnFileSystem<T>(id, fs, serializer);
			await o.InitializeAsync();
			return o;
		}

		public virtual async Task InitializeAsync()
		{
			await Fs.SetCurrentDirectoryAsync(null);
		}

		protected async Task<IFileSystem> GetFsAsync()
		{
			await Fs.SetCurrentDirectoryAsync(null);
			await Fs.CreateDirectoryAsync(Id);
			return Fs;
		}

		const string ModifiedOnSeparator = "@";

		public virtual async Task StoreAsync(string thingId, T data, DateTime modifiedOn)
		{
			if (Serializer == null)
				throw new SerializerNotSetException();
			
			string d;
			if (typeof(T).Equals(typeof(string)))
				d = data.ToString();
			else
				d = Serializer.ToString(data);

			string mod = Serializer.ToString(modifiedOn);

			IFileSystem fs = await GetFsAsync();
			await fs.WriteTextAsync(thingId, mod + ModifiedOnSeparator + d);
		}

		public virtual async Task<bool> ExistsAsync(string thingId)
		{
			// We do not use GetFsAsync() in order to delay directory creation as much as possible.

			await Fs.SetCurrentDirectoryAsync(null);
			if (!await Fs.DirectoryExistsAsync(Id))
				return false;

			await Fs.CreateDirectoryAsync(Id);
			return await Fs.FileExistsAsync(thingId);
		}

		public virtual async Task<DateTime> GetModifiedOnAsync(string thingId)
		{
			if (!await ExistsAsync(thingId))
				return DateTime.MinValue;

			if (Serializer == null)
				throw new SerializerNotSetException();

			IFileSystem fs = await GetFsAsync();
			string sd = await fs.ReadTextAsync(thingId);
			sd = sd.Substring(0, sd.IndexOf(ModifiedOnSeparator, StringComparison.Ordinal));

			return Serializer.FromString<DateTime>(sd);
		}

		public virtual async Task<T> GetACopyAsync(string thingId)
		{
			if (!await ExistsAsync(thingId))
				return null;

			// Get data from file system and skip saved modification date (is not needed here)
			IFileSystem fs = await GetFsAsync();
			string sd = await fs.ReadTextAsync(thingId);
			sd = sd.Substring(sd.IndexOf(ModifiedOnSeparator, StringComparison.Ordinal) + ModifiedOnSeparator.Length);

			// Restore object
			object d = sd;
			if (typeof(T).Equals(typeof(string)))
				return (T)d;
			else
			{
				if (Serializer == null)
					throw new SerializerNotSetException();
				
				return Serializer.FromString<T>((string)d);
			}
		}

		public virtual async Task<T> RetrieveAsync(string thingId)
		{
			T data = await GetACopyAsync(thingId);
			if (data != null)
				await DiscardAsync(thingId);
			return data;
		}

		public virtual async Task DiscardAsync(string thingId)
		{
			IFileSystem fs = await GetFsAsync();
			await fs.DeleteFileAsync(thingId);
		}


		//

		public virtual async Task<IEnumerable<string>> FindIdsAsync(string pattern)
		{
			// We do not use GetFsAsync() in order to delay directory creation as much as possible.

			await Fs.SetCurrentDirectoryAsync(null);
			if (!await Fs.DirectoryExistsAsync(Id))
				return null;

			await Fs.CreateDirectoryAsync(Id);
			return await Fs.GetFilesAsync(pattern);
		}
	}
}
