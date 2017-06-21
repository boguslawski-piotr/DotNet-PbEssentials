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
}
