#if __ANDROID__

using Android.Content;
using Android.Runtime;
using Android.Security.Keystore;
using Android.Views;
using Java.Security;
using Javax.Crypto;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
	public static partial class DeviceEx
	{
		static string _Id
		{
			get {
				const string KEY_NAME = "a10a05d2-1b61-4485-8395-08eeff4d00da";
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

		static DeviceOrientation _Orientation
		{
			get {
				IWindowManager windowManager = Android.App.Application.Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

				var rotation = windowManager.DefaultDisplay.Rotation;
				bool isLandscape = rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270;

				return isLandscape ? DeviceOrientation.Landscape : DeviceOrientation.Portrait;
			}
		}

		static bool _StatusBarVisible => Device.Idiom != TargetIdiom.Desktop;
	}
}

#endif