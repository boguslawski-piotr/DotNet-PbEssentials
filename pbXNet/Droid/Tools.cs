#if __ANDROID__

using Android.OS;
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
				string id, id2;
				if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
				{
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

					id = mid.ToHexString();
					id2 = Android.Provider.Settings.Secure.AndroidId;
				}
				else
				{
					id = Android.Provider.Settings.Secure.AndroidId;
					id2 = "3AC2B00B1DB64686B388EEEF2F708009";
				}

				Log.D($"id={id}, id2={id2}");
				return id + id2;
			}
		}
	}
}

#endif