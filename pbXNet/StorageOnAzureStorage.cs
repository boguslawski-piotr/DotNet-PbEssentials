using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace pbXNet
{
	// TODO: implement StorageOnAzureStorage<T>

	// simple example: https://github.com/xamarin/xamarin-forms-samples/blob/master/WebServices/AzureStorage/FileUploader/AzureStorage.cs

	// emulator: https://docs.microsoft.com/en-us/azure/storage/storage-use-emulator

	// connection string: https://docs.microsoft.com/en-us/azure/storage/storage-configure-connection-string

	public class AzureStorageSettings
	{
		public string ConnectionString;
	}

	public class StorageOnAzureStorage<T> : Storage<T>, ISearchableStorage<T> where T : class
	{
		public override StorageType Type => StorageType.RemoteService;

		public override string Name => Id;

		protected AzureStorageSettings Settings;
		protected CloudStorageAccount Account;
		protected CloudBlobClient Client;
		protected CloudBlobContainer Container;

		public StorageOnAzureStorage(string id, AzureStorageSettings settings, ISerializer serializer)
			: base(id, serializer)
		{
			Settings = settings;
		}

		public static async Task<StorageOnAzureStorage<T>> NewAsync(string id, AzureStorageSettings settings, ISerializer serializer)
		{
			StorageOnAzureStorage<T> o = new StorageOnAzureStorage<T>(id, settings, serializer);
			await o.InitializeAsync().ConfigureAwait(false);
			return o;
		}

		const string _modifiedOnAttribute = "modifiedOn";

		public override async Task InitializeAsync()
		{
			Account = CloudStorageAccount.Parse(Settings.ConnectionString);
			Client = Account.CreateCloudBlobClient();

			string containerId = Regex.Replace(Id.ToLower(), "[^a-z0-9]", "").ToLower();
			Container = Client.GetContainerReference(containerId);

			await Container.CreateIfNotExistsAsync();
		}

		public override async Task StoreAsync(string thingId, T data, DateTime modifiedOn)
		{
			var blob = Container.GetBlockBlobReference(thingId);

			ByteBuffer bdata = new ByteBuffer(Serializer.Serialize<T>(data), Encoding.UTF8);
			await blob.UploadFromByteArrayAsync(bdata, 0, bdata.Length).ConfigureAwait(false);

			blob.Metadata[_modifiedOnAttribute] = Serializer.Serialize<DateTime>(modifiedOn.ToUniversalTime());
			await blob.SetMetadataAsync().ConfigureAwait(false);
		}

		public override async Task<bool> ExistsAsync(string thingId)
		{
			var blob = Container.GetBlobReference(thingId);
			return await blob.ExistsAsync().ConfigureAwait(false);
		}

		public override async Task<DateTime> GetModifiedOnAsync(string thingId)
		{
			var blob = Container.GetBlobReference(thingId);
			if (!await blob.ExistsAsync().ConfigureAwait(false))
				return DateTime.MinValue;

			await blob.FetchAttributesAsync().ConfigureAwait(false);

			return Serializer.Deserialize<DateTime>(blob.Metadata[_modifiedOnAttribute]);
		}

		public override async Task<T> GetACopyAsync(string thingId)
		{
			var blob = Container.GetBlobReference(thingId);
			if (!await blob.ExistsAsync().ConfigureAwait(false))
				return null;

			await blob.FetchAttributesAsync().ConfigureAwait(false);

			byte[] blobBytes = new byte[blob.Properties.Length];
			await blob.DownloadToByteArrayAsync(blobBytes, 0).ConfigureAwait(false);

			return Serializer.Deserialize<T>(Encoding.UTF8.GetString(blobBytes));
		}

		public override async Task DiscardAsync(string thingId)
		{
			var blob = Container.GetBlobReference(thingId);
			await blob.DeleteIfExistsAsync().ConfigureAwait(false);
		}

		public virtual async Task<IEnumerable<string>> FindIdsAsync(string pattern)
		{
			List<string> ids = null;
			BlobContinuationToken token = null;

			do
			{
				var segment = await Container.ListBlobsSegmentedAsync(token);
				if (segment.Results.Count() > 0)
				{
					var idsSegment = segment.Results.Cast<CloudBlockBlob>().Where(b => Regex.IsMatch(b.Name, pattern)).Select(b => b.Name);
					if (idsSegment.Count() > 0)
					{
						if(ids == null)
							ids = new List<string>();
						ids.AddRange(idsSegment);
					}
				}

				token = segment.ContinuationToken;

			} while (token != null);

			return ids;
		}
	}
}
