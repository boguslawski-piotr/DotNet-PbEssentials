using System;
using System.Threading.Tasks;

namespace pbXNet
{
    public interface IStorage<T>
    {
		/// <summary>
		/// Adds or update data identified by id in the storage
		/// </summary>
		Task StoreAsync(string id, T data);

		/// <summary>
		/// Determines whether data identified by id exists in the storage
		/// </summary>
		Task<bool> ExistsAsync(string id);

        /// <summary>
		/// Gets a copy of data identified by id from the storage
		/// If value with the given id does not exist, returns null
		/// </summary>
		Task<T> GetACopyAsync(string id);

		/// <summary>
		/// Retrieves (== after this operation data is no longer stored) data identified by id from the storage
		/// If value with the given id does not exist, returns null
		/// </summary>
		Task<T> RetrieveAsync(string id);

		/// <summary>
		/// Deletes data identified by id from the storage
		/// </summary>
		Task DiscardAsync(string id);
	}

    public interface ISearchableStorage<T> : IStorage<T>
    {
    }

}
