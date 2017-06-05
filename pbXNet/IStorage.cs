using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pbXNet
{
	public interface IStorage<T> where T : class
	{
		string Id { get; }

		string Name { get; }

		/// <summary>
		/// Adds or update data identified by thingId in the storage.
		/// </summary>
		Task StoreAsync(string thingId, T data, DateTime modifiedOn);

		/// <summary>
		/// Determines whether data identified by thingId exists in the storage.
		/// </summary>
		Task<bool> ExistsAsync(string thingId);

		/// <summary>
		/// Returns modification date/time stored with data (see StoreAsync).
		/// If value with the given thingId does not exist, should return DateTime.MinValue.
		/// </summary>
		Task<DateTime> GetModifiedOnAsync(string thingId);

		/// <summary>
		/// Gets a copy of data identified by thingId from the storage.
		/// If value with the given thingId does not exist, should return null.
		/// </summary>
		Task<T> GetACopyAsync(string thingId);

		/// <summary>
		/// Retrieves (== after this operation data is no longer stored) data identified by thingId from the storage.
		/// If value with the given thingId does not exist, should return null.
		/// </summary>
		Task<T> RetrieveAsync(string thingId);

		/// <summary>
		/// Deletes data identified by thingId from the storage.
		/// </summary>
		Task DiscardAsync(string thingId);
	}

	public interface ISearchableStorage<T> : IStorage<T> where T : class
	{
		/// <summary>
		/// Finds the identifiers matching a pattern (.NET Regex).
		/// If nothing was found, should return null.
		/// </summary>
		Task<IEnumerable<string>> FindIdsAsync(string pattern);
	}

}
