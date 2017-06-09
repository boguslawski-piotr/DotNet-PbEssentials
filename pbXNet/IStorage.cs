using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pbXNet
{
	public interface IStorage<T> where T : class
	{
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
