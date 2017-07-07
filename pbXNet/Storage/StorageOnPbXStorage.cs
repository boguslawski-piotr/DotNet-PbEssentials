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

	public enum PbXStorageErrorCode
	{
		IncorrectResponseData = 1,
		CommandUnrecognized = 2,
		SystemException = 3,

		// 100 - 600: HttpStatusCode

		RepositoryDoesNotExist = 1000,
		AppRegistrationFailed = 1001,
		IncorrectAppToken = 2000,
		OpenStorageFailed = 2001,
		IncorrectStorageToken = 3000,
		ThingNotFound = 3001,
		ThingOperationFailed = 3002,
	}

	public class StorageOnPbXStorageException : StorageException
	{
		public int Erorr;

		public StorageOnPbXStorageException(int error, string message)
			: base($"{error}, {message.Trim()}")
		{
			Erorr = error;
		}

		public StorageOnPbXStorageException(PbXStorageErrorCode error, string message)
			: this((int)error, message)
		{ }
	}

	public class StorageOnPbXStorage
	{
		#region Tools

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
					default:
						throw new StorageOnPbXStorageException(PbXStorageErrorCode.CommandUnrecognized, T.Localized("SOPXS_CmdUnrecognized", cmd));
				}

				if (response.IsSuccessStatusCode)
				{
					var responseContent = await response.Content.ReadAsStringAsync();
					responseContent = Obfuscator.DeObfuscate(responseContent);

					Log.D($"RESPONSE: {responseContent}");

					string[] contentData = responseContent.Split(commaAsArray, 2);
					if (contentData[0] == "ERROR" && contentData.Length > 1)
					{
						string[] errorData = contentData[1].Split(commaAsArray, 2);
						throw new StorageOnPbXStorageException(int.Parse(errorData[0]), errorData[1]);
					}
					else if (contentData[0] != "OK")
					{
						throw new StorageOnPbXStorageException(PbXStorageErrorCode.IncorrectResponseData, T.Localized("SOPXS_IncorrectData"));
					}

					return contentData.Length > 1 ? contentData[1] : null;
				}
				else
					throw new StorageOnPbXStorageException((int)response.StatusCode, T.Localized("SOPXS_IncorrectData"));
			}
			catch (StorageOnPbXStorageException ex)
			{
				Log.E(ex);
				if (ex.Erorr == (int)PbXStorageErrorCode.ThingNotFound)
					throw new StorageThingNotFoundException(ex.Message, null);
				else
					throw ex;
			}
			catch (Exception ex)
			{
				string message = Log.E(ex);
				throw new StorageOnPbXStorageException(PbXStorageErrorCode.SystemException, message);
			}
		}

		#endregion
	}
}
