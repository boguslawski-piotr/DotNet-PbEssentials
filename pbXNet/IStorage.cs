using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pbXNet
{
    public interface IStorage<T> where T: class
    {
		string Id { get; }
		
        string Name { get; }

        /// <summary>
		/// Adds or update data identified by id in the storage.
		/// </summary>
        Task StoreAsync(string id, T data, DateTime modifiedOn);

		/// <summary>
		/// Determines whether data identified by id exists in the storage.
		/// </summary>
		Task<bool> ExistsAsync(string id);

		/// <summary>
		/// Returns modification date/time stored with data (see StoreAsync).
		/// If value with the given id does not exist, should return DateTime.MinValue.
		/// </summary>
		Task<DateTime> GetModifiedOnAsync(string id);

		/// <summary>
		/// Gets a copy of data identified by id from the storage.
		/// If value with the given id does not exist, should return null.
		/// </summary>
		Task<T> GetACopyAsync(string id);

		/// <summary>
		/// Retrieves (== after this operation data is no longer stored) data identified by id from the storage.
		/// If value with the given id does not exist, should return null.
		/// </summary>
		Task<T> RetrieveAsync(string id);

		/// <summary>
		/// Deletes data identified by id from the storage.
		/// </summary>
		Task DiscardAsync(string id);
	}

    public interface ISearchableStorage<T> : IStorage<T> where T : class
    {
        Task<IEnumerable<string>> FindIdsAsync(string pattern);
    }

}
