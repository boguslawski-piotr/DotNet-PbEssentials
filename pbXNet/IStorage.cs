using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pbXNet
{
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

	public interface IStorage<T> where T : class
	{
		StorageType Type { get; }

		string Id { get; }

		string Name { get; }

		Task InitializeAsync();

		/// Adds or update data identified by thingId in the storage.
		Task StoreAsync(string thingId, T data, DateTime modifiedOn);

		/// Determines whether data identified by thingId exists in the storage.
		Task<bool> ExistsAsync(string thingId);

		/// Returns modification date/time stored with data (see StoreAsync).
		/// If value with the given thingId does not exist, should return DateTime.MinValue.
		Task<DateTime> GetModifiedOnAsync(string thingId);

		/// Gets a copy of data identified by thingId from the storage.
		/// If value with the given thingId does not exist, should return null.
		Task<T> GetACopyAsync(string thingId);

		/// Retrieves (== after this operation data is no longer stored) data identified by thingId from the storage.
		/// If value with the given thingId does not exist, should return null.
		Task<T> RetrieveAsync(string thingId);

		/// Deletes data identified by thingId from the storage.
		Task DiscardAsync(string thingId);
	}

	public interface ISearchableStorage<T> : IStorage<T> where T : class
	{
		/// Finds the identifiers matching a pattern (.NET Regex).
		/// If nothing was found, should return null.
		Task<IEnumerable<string>> FindIdsAsync(string pattern);
	}

}
