using System;
using System.Threading.Tasks;

namespace pbXNet
{
	public abstract class Storage<T> : IStorage<T>
	{
		public abstract StorageType Type { get; }

		public string Id { get; private set; }

		public abstract string Name { get; }

		protected ISerializer Serializer;

		protected Storage(string id, ISerializer serializer = null)
		{
			if (id == null)
				throw new ArgumentNullException(nameof(id));
			
			Id = id;
			Serializer = new StringOptimizedSerializer(serializer ?? new NewtonsoftJsonSerializer());
		}

		public abstract Task InitializeAsync();

		public abstract Task StoreAsync(string thingId, T data, DateTime modifiedOn);

		public abstract Task<bool> ExistsAsync(string thingId);

		public abstract Task<DateTime> GetModifiedOnAsync(string thingId);

		public abstract Task<T> GetACopyAsync(string thingId);

		public abstract Task DiscardAsync(string thingId);

		public virtual async Task<T> RetrieveAsync(string thingId)
		{
			T data = await GetACopyAsync(thingId).ConfigureAwait(false);
			await DiscardAsync(thingId).ConfigureAwait(false);
			return data;
		}
	}
}