#if WINDOWS_UWP

using System;

namespace pbXNet
{
	public static partial class Tools
	{
		static string _Uaqpid
		{
			get {
				// MAX 10 passwords in valut per app
				// can be used as SecureStorage? -> jeden passwd, w ktorym jest zapisane cale storage :)

				var vault = new PasswordVault();

				PasswordCredential cred = vault.Retrieve(resource, userName);
				if (cred == null)
				{
					var cred = new PasswordCredential(resource, userName, new Password(Tools.CreateGuid()));
					vault.Add(cred);
				}

				cred.RetrievePassword();
				string id = cred.Password;
				
string id2 = "fd5d013709f94b4494fdeda98535fd32";

				return id + id2;
			}
		}
	}

	[AttributeUsageAttribute(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
	public sealed class SerializableAttribute : Attribute { }
}

#endif