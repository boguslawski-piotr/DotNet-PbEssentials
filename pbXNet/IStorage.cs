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

		Task<bool> InitializeAsync();

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
		/// If data with the given <paramref name="thingId"/> does not exist, should return DateTime.MinValue.
		/// </summary>
		Task<DateTime> GetModifiedOnAsync(string thingId);

		/// <summary>
		/// Gets a copy of data identified by <paramref name="thingId"/> from the storage.
		/// If data with the given <paramref name="thingId"/> does not exist, should return null.
		/// </summary>
		Task<T> GetACopyAsync(string thingId);

		/// <summary>
		/// Retrieves (== after this operation data is no longer stored) data identified by <paramref name="thingId"/> from the storage.
		/// If data with the given <paramref name="thingId"/> does not exist, should return null.
		/// </summary>
		Task<T> RetrieveAsync(string thingId);

		/// <summary>
		/// Deletes data identified by <paramref name="thingId"/> from the storage.
		/// </summary>
		Task DiscardAsync(string thingId);
	}

	public interface ISearchableStorage<T> : IStorage<T> where T : class
	{
		/// <summary>
		/// Finds the identifiers matching a <paramref name="pattern"/> (.NET Regex).
		/// If nothing was found, should return null.
		/// </summary>
		Task<IEnumerable<string>> FindIdsAsync(string pattern);
	}

}
