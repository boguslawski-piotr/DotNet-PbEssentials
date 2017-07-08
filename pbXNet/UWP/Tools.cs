#if WINDOWS_UWP

using System;
using Windows.Security.Credentials;

namespace pbXNet
{
	public static partial class Tools
	{
		static string _Uaqpid
		{
			get {
				// WARNING: MAX 10 passwords in valut per app.
				// TODO: can be used as SecureStorage? -> jeden passwd, w ktorym jest zapisane cale storage :)

				const string resource = ".8bf336b952fd4e8d97e17b7c7e96b73a";
				const string userName = "0";

				var vault = new PasswordVault();
				PasswordCredential cred = null;
				try
				{ 
					cred = vault.Retrieve(resource, userName);
				}
				catch { }
				if (cred == null)
				{
					cred = new PasswordCredential(resource, userName, Tools.CreateGuid());
					vault.Add(cred);
				}

				cred.RetrievePassword();

				string id = cred.Password;
				string id2 = "UWP";

				return id + id2;
			}
		}
	}
}

#endif