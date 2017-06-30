using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet
{
	[System.Serializable]
	public class PbXStorageSettings
	{
		public IAsymmetricCryptographerKeyPair AppKeys;
		
		public string RepositoryId;
		public IAsymmetricCryptographerKeyPair RepositoryPublicKey;

		// For example: http[s]://[pbXStorage server address]:[pbXStorage server port]/api/storage/
		public Uri ApiUri;

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
		public static readonly char[] commaAsArray = { ',' };

		public static async Task<string> ExecuteCommandAsync(HttpClient httpClient, string cmd, Uri uri, HttpContent content = null)
		{
			try
			{
				Log.D($"REQUEST: {cmd}: {uri}");

				HttpResponseMessage response = null;
				switch (cmd)
				{
					case "GET":
						response = await httpClient.GetAsync(uri);
						break;
					case "POST":
						response = await httpClient.PostAsync(uri, content);
						break;
					case "PUT":
						response = await httpClient.PutAsync(uri, content);
						break;
					case "DELETE":
						response = await httpClient.DeleteAsync(uri);
						break;
				}

				if (response != null)
				{
					if (response.IsSuccessStatusCode)
					{
						var responseContent = await response.Content.ReadAsStringAsync();
						responseContent = Obfuscator.DeObfuscate(responseContent);

						Log.D($"RESPONSE: {responseContent}");

						string[] contentData = responseContent.Split(commaAsArray, 2);
						if (contentData[0] == "ERROR")
						{
							throw new StorageOnPbXStorageException(contentData[1]);
						}
						else if (contentData[0] != "OK")
						{
							throw new StorageOnPbXStorageException("1,Incorrect data.");
						}

						return contentData.Length > 1 ? contentData[1] : null;
					}
					else
						throw new StorageOnPbXStorageException($"{response.StatusCode},Failed to read data.");
				}
				else
					throw new StorageOnPbXStorageException($"2,Command {cmd} unrecognized.");
			}
			catch (StorageOnPbXStorageException ex)
			{
				Log.E(ex.Message);
				throw ex;
			}
			catch (Exception ex)
			{
				string message = $"3,{ex.Message}";
				if (ex.InnerException != null)
					message += $" {ex.InnerException.Message + (ex.InnerException.Message.EndsWith(".") ? "" : ".")}";
				Log.E(message);
				throw new StorageOnPbXStorageException(message);
			}
		}
	}
}
