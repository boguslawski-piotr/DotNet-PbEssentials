#if __ANDROID__

using Android.Security.Keystore;
using Java.Security;
using Javax.Crypto;

namespace pbXNet
{
	public static partial class Tools
	{
		static string _Uaqpid
		{
			get {
				const string KEY_NAME = ".a10a05d21b614485839508eeff4d00da";
				const string KEYSTORE_NAME = "AndroidKeyStore";

				KeyStore keystore = KeyStore.GetInstance(KEYSTORE_NAME);
				keystore.Load(null);

				if (!keystore.IsKeyEntry(KEY_NAME))
				{
					KeyGenerator keyGenerator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmHmacSha256, KEYSTORE_NAME);
					keyGenerator.Init(new KeyGenParameterSpec.Builder(KEY_NAME, KeyStorePurpose.Sign).Build());
					keyGenerator.GenerateKey();
				}

				IKey secretKey = keystore.GetKey(KEY_NAME, null);
				Mac mac = Mac.GetInstance(KeyProperties.KeyAlgorithmHmacSha256);
				mac.Init(secretKey);
				byte[] mid = mac.DoFinal();

				string id = mid.ToHexString();
				string id2 = Android.Provider.Settings.Secure.AndroidId;

				return id + id2;
			}
		}
	}
}

#endif