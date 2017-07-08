using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pbXNet
{
	public class StorageException : Exception
	{
		public StorageException(string message)	: base(message)	{ }
	}

	public class StorageThingNotFoundException : StorageException
	{
		public StorageThingNotFoundException(string thingId) : base(pbXNet.T.Localized("S_ThingNotFound", thingId)) { }
		public StorageThingNotFoundException(string message, string thingId) : base(string.Format(message, thingId)) { }
	}

	[Flags]
	public enum StorageType
	{
		Memory = 0x0000,
		LocalIO = 0x0001,
		LocalService = 0x0002,
		Quick = Memory | LocalIO | LocalService,
		RemoteIO = 0x0010,
		RemoteService = 0x0020,
		Slow = RemoteIO | RemoteService,
	}

	public interface IStorage<T>
	{
		StorageType Type { get; }

		string Id { get; }

		string Name { get; }

		Task InitializeAsync();

		/// <summary>
		/// Adds or update data identified by <paramref name="thingId"/> in the storage. 
		/// Converts <paramref name="modifiedOn"/> to UTC and stores with data.
		/// </summary>
		Task StoreAsync(string thingId, T data, DateTime modifiedOn);

		/// <summary>
		/// Determines whether data identified by <paramref name="thingId"/> exists in the storage.
		/// </summary>
		Task<bool> ExistsAsync(string thingId);

		/// <summary>
		/// Returns modification date/time (as UTC) stored with data (see <see cref="StoreAsync">StoreAsync</see>).
		/// If data with the given <paramref name="thingId"/> does not exist, should throw StorageThingNotFoundException.
		/// </summary>
		Task<DateTime> GetModifiedOnAsync(string thingId);

		/// <summary>
		/// Gets a copy of data identified by <paramref name="thingId"/> from the storage.
		/// If data with the given <paramref name="thingId"/> does not exist, should throw StorageThingNotFoundException.
		/// </summary>
		Task<T> GetACopyAsync(string thingId);

		/// <summary>
		/// Retrieves (== after this operation data is no longer stored) data identified by <paramref name="thingId"/> from the storage.
		/// If data with the given <paramref name="thingId"/> does not exist, should throw StorageThingNotFoundException.
		/// </summary>
		Task<T> RetrieveAsync(string thingId);

		/// <summary>
		/// Deletes data identified by <paramref name="thingId"/> from the storage.
		/// </summary>
		Task DiscardAsync(string thingId);
	}

	public interface ISearchableStorage<T> : IStorage<T>
	{
		/// <summary>
		/// Finds the identifiers matching a <paramref name="pattern"/> (.NET Regex).
		/// If nothing was found, should return empty IEnumerable&lt;string&gt;.
		/// </summary>
		Task<IEnumerable<string>> FindIdsAsync(string pattern);
	}

}
