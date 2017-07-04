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
	public class AzureStorageSettings
	{
		public enum StorageType
		{
			BlockBlob,
			PageBlob,
		}

		/// Connection string: https://docs.microsoft.com/en-us/azure/storage/storage-configure-connection-string
		public string ConnectionString;

		public StorageType Type;
	}

	public class StorageOnAzureStorage<T> : Storage<T>, ISearchableStorage<T> where T : class
	{
		public override StorageType Type => StorageType.RemoteService;

		public override string Name => Id;

		protected AzureStorageSettings Settings;
		protected CloudStorageAccount Account;
		protected CloudBlobClient Client;
		protected CloudBlobContainer Container;

		/// Id will be used as container name in Azure Blob Storage
		/// See here: https://azure.microsoft.com/en-us/services/storage/blobs/
		public StorageOnAzureStorage(string id, AzureStorageSettings settings, ISerializer serializer = null)
			: base(id, serializer)
		{
			if (settings.Type != AzureStorageSettings.StorageType.BlockBlob && settings.Type != AzureStorageSettings.StorageType.PageBlob)
				throw new ArgumentOutOfRangeException(nameof(Settings.Type));

			Settings = settings;
		}

		public static async Task<StorageOnAzureStorage<T>> NewAsync(string id, AzureStorageSettings settings, ISerializer serializer = null)
		{
			StorageOnAzureStorage<T> o = new StorageOnAzureStorage<T>(id, settings, serializer);
			await o.InitializeAsync();
			return o;
		}

		const string _dataSizeAttribute = "dataSize";
		const string _modifiedOnAttribute = "modifiedOn";

		public override async Task InitializeAsync()
		{
			try
			{
				Account = CloudStorageAccount.Parse(Settings.ConnectionString);
				Client = Account.CreateCloudBlobClient();

				/*
					A container name must be a valid DNS name, conforming to the following naming rules:
					  - Container names must start with a letter or number, and can contain only 
					    letters, numbers, and the dash (-) character.
					  - Every dash (-) character must be immediately preceded and followed 
					    by a letter or number; consecutive dashes are not permitted in container names.
					  - All letters in a container name must be lowercase.
					  - Container names must be from 3 through 63 characters long.
				*/
				string containerId = Regex.Replace(Id.ToLower(), "[^a-z0-9-]", "-");
				containerId = Regex.Replace(containerId, "^-", "x");
				containerId = Regex.Replace(containerId, "-$", "x");
				containerId = Regex.Replace(containerId, "-{2,}", "-");
				if (containerId.Length < 3)
					containerId = containerId.PadLeft(3, 'x');
				if (containerId.Length > 63)
					containerId = containerId.Remove(63);

				Container = Client.GetContainerReference(containerId);

				await Container.CreateIfNotExistsAsync();
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				throw ex;
			}
		}

		public override async Task StoreAsync(string thingId, T data, DateTime modifiedOn)
		{
			try
			{
				byte[] bdata = Encoding.UTF8.GetBytes(Serializer.Serialize<T>(data));
				int dataSize = bdata.Length;

				CloudBlob blob;
				if (Settings.Type == AzureStorageSettings.StorageType.BlockBlob)
				{
					blob = Container.GetBlockBlobReference(thingId);
					CloudBlockBlob blockBlob = blob as CloudBlockBlob;

					await blockBlob.UploadFromByteArrayAsync(bdata, 0, dataSize).ConfigureAwait(false);
				}
				else if (Settings.Type == AzureStorageSettings.StorageType.PageBlob)
				{
					blob = Container.GetPageBlobReference(thingId);
					CloudPageBlob pageBlob = blob as CloudPageBlob;

					int blobSize = 512 * (dataSize / 512 + 1);
					Array.Resize(ref bdata, blobSize);

					await pageBlob.CreateAsync(blobSize).ConfigureAwait(false);
					await pageBlob.UploadFromByteArrayAsync(bdata, 0, blobSize).ConfigureAwait(false);

					blob.Metadata[_dataSizeAttribute] = dataSize.ToString();
				}
				else
					throw new ArgumentOutOfRangeException(nameof(Settings.Type));

				blob.Metadata[_modifiedOnAttribute] = Serializer.Serialize<DateTime>(modifiedOn.ToUniversalTime());
				await blob.SetMetadataAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				throw ex;
			}
		}

		public override async Task<bool> ExistsAsync(string thingId)
		{
			try
			{
				var blob = Container.GetBlobReference(thingId);
				return await blob.ExistsAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				throw ex;
			}
		}

		public override async Task<DateTime> GetModifiedOnAsync(string thingId)
		{
			try
			{
				var blob = Container.GetBlobReference(thingId);
				if (!await blob.ExistsAsync().ConfigureAwait(false))
					return DateTime.MinValue;

				await blob.FetchAttributesAsync().ConfigureAwait(false);

				return Serializer.Deserialize<DateTime>(blob.Metadata[_modifiedOnAttribute]);
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				throw ex;
			}
		}

		public override async Task<T> GetACopyAsync(string thingId)
		{
			try
			{
				var blob = Container.GetBlobReference(thingId);
				if (!await blob.ExistsAsync().ConfigureAwait(false))
					return null;

				await blob.FetchAttributesAsync().ConfigureAwait(false);

				byte[] blobBytes = new byte[blob.Properties.Length];
				await blob.DownloadToByteArrayAsync(blobBytes, 0).ConfigureAwait(false);

				int dataSize = 0;
				if (Settings.Type == AzureStorageSettings.StorageType.BlockBlob)
					dataSize = blobBytes.Length;
				else if (Settings.Type == AzureStorageSettings.StorageType.PageBlob)
					dataSize = int.Parse(blob.Metadata[_dataSizeAttribute]);
				else
					throw new ArgumentOutOfRangeException(nameof(Settings.Type));

				return Serializer.Deserialize<T>(Encoding.UTF8.GetString(blobBytes, 0, dataSize));
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				throw ex;
			}
		}

		public override async Task DiscardAsync(string thingId)
		{
			try
			{
				var blob = Container.GetBlobReference(thingId);
				await blob.DeleteIfExistsAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				throw ex;
			}
		}

		public virtual async Task<IEnumerable<string>> FindIdsAsync(string pattern)
		{
			try
			{
				List<string> ids = null;
				BlobContinuationToken token = null;

				do
				{
					var segment = await Container.ListBlobsSegmentedAsync(token);
					if (segment?.Results.Count() > 0)
					{
						var idsSegment = segment.Results.Cast<CloudBlob>().Where(b => Regex.IsMatch(b.Name, pattern)).Select(b => b.Name);
						if (idsSegment?.Count() > 0)
						{
							if (ids == null)
								ids = new List<string>();
							ids.AddRange(idsSegment);
						}
					}

					token = segment?.ContinuationToken;

				} while (token != null);

				return ids;
			}
			catch (Exception ex)
			{
				Log.E(ex.Message, this);
				throw ex;
			}
		}
	}
}
