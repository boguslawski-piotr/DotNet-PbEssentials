using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pbXNet
{
	// TODO: implement StorageOnAzureStorage<T>

	// simple example: https://github.com/xamarin/xamarin-forms-samples/blob/master/WebServices/AzureStorage/FileUploader/AzureStorage.cs

	// emulator: https://docs.microsoft.com/en-us/azure/storage/storage-use-emulator

	// connection string: https://docs.microsoft.com/en-us/azure/storage/storage-configure-connection-string

	public class StorageOnAzureStorage<T> : Storage<T>, ISearchableStorage<T> where T : class
	{
		public override StorageType Type => StorageType.RemoteService;

		public override string Name => _containerName; // TODO: dodac cos jeszcze do nazwy aby bylo widac, ze to Azure

		string _connectionString;
		string _containerName;

		public StorageOnAzureStorage(string id, string connectionString, string containerName, ISerializer serializer)
			: base(id, serializer)
		{
			_connectionString = connectionString;
			_containerName = containerName;
		}

		public static async Task<StorageOnAzureStorage<T>> NewAsync(string id, string connectionString, string containerName, ISerializer serializer)
		{
			StorageOnAzureStorage<T> o = new StorageOnAzureStorage<T>(id, connectionString, containerName, serializer);
			await o.InitializeAsync().ConfigureAwait(false);
			return o;
		}

		public override async Task InitializeAsync()
		{
			//var account = CloudStorageAccount.Parse(_connectionString);
			//var client = account.CreateCloudBlobClient();
			//var container = client.GetContainerReference(_containerName);
		}

		public override async Task StoreAsync(string thingId, T data, DateTime modifiedOn)
		{
			// store: thingId, Serialize(data)
		}

		public override async Task<bool> ExistsAsync(string thingId)
		{
			return false;
		}

		public override async Task<DateTime> GetModifiedOnAsync(string thingId)
		{
			if (!await ExistsAsync(thingId).ConfigureAwait(false))
				return DateTime.MinValue;

			return DateTime.MinValue;
		}

		public override async Task<T> GetACopyAsync(string thingId)
		{
			if (!await ExistsAsync(thingId).ConfigureAwait(false))
				return null;

			// return: thingId, Deserialize(data)

			return null;
		}

		public override async Task DiscardAsync(string thingId)
		{
		}

		public virtual async Task<IEnumerable<string>> FindIdsAsync(string pattern)
		{
			return null;
		}
	}
}
