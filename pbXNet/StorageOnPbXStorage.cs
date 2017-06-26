using System;
using System.Collections.Generic;
using System.Text;

namespace pbXNet
{
	[System.Serializable]
	public class PbXStorageSettings
	{
		// For example: http[s]://[pbXStorage server address]:[pbXStorage server port]/api/storage/
		public Uri ApiUri;

		public IAsymmetricCryptographerKeyPair AppKeys;

		public string ClientId;
		public IAsymmetricCryptographerKeyPair ClientPublicKey;

		public TimeSpan Timeout = TimeSpan.FromSeconds(30);
	}

	public class StorageOnPbXStorageException : Exception
	{
		public StorageOnPbXStorageException(string message)
			: base(message)
		{ }
	}

	public class StorageOnPbXStorage
	{
	}
}
