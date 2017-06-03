#if __ANDROID__

using System;
using System.Diagnostics;
using Android;
using Android.App;
using Android.Hardware.Fingerprints;
using Android.Security.Keystore;
using Android.Support.V4.Content;
using Android.Support.V4.Hardware.Fingerprint;
using Android.Support.V4.OS;
using Android.Support.V7.App;
using Android.Util;
using Java.Lang;
using Java.Security;
using Javax.Crypto;

namespace pbXSecurity
{

	public partial class SecretsManager : ISecretsManager
	{
		public void Initialize(object activity)
		{
			_activity = activity as AppCompatActivity;
			if (_activity == null)
				return;

			_fingerprintManager = FingerprintManagerCompat.From(_activity);
			if (_fingerprintManager.IsHardwareDetected)
			{
				KeyguardManager keyguardManager = (KeyguardManager)_activity.GetSystemService(Android.Content.Context.KeyguardService);
				if (keyguardManager.IsKeyguardSecure)
				{
					if (_fingerprintManager.HasEnrolledFingerprints)
					{
						_deviceOwnerAuthenticationWithBiometricsAvailable = true;
					}
				}
			}
		}

		public bool DeviceOwnerAuthenticationAvailable => DeviceOwnerAuthenticationWithBiometricsAvailable;
		public bool DeviceOwnerAuthenticationWithBiometricsAvailable => _deviceOwnerAuthenticationWithBiometricsAvailable;

		bool _deviceOwnerAuthenticationWithBiometricsAvailable;
		Activity _activity;

		CryptoObjectHelper _cryptoObjectHelper = new CryptoObjectHelper();
		FingerprintManagerCompat _fingerprintManager;
		FingerprintAuthCallbacks _callbacks;
		CancellationSignal _cancellationSignal;

		public bool AuthenticateDeviceOwner(string msg, Action Success, Action<string, bool> Error)
		{
			if (!DeviceOwnerAuthenticationWithBiometricsAvailable || _activity == null)
				return false;

			Android.Content.PM.Permission permissionResult = ContextCompat.CheckSelfPermission(_activity, Manifest.Permission.UseFingerprint);
			if (permissionResult != Android.Content.PM.Permission.Granted)
			{
				if (_activity.ShouldShowRequestPermissionRationale(Manifest.Permission.UseFingerprint))
				{
					// TODO: co tu wyswietlic?
					Error("Show an explanation to the user... TODO", false);
				}
				else
				{
					_activity.RequestPermissions(new string[] { Manifest.Permission.UseFingerprint }, 1);

					// TODO: tutaj dalej wg. tego poradnika :(
					// https://developer.android.com/training/permissions/requesting.html

					Error("No permission to use fingerprint scanner.", false);
					return true;
				}
			}

			_fingerprintManager = FingerprintManagerCompat.From(_activity);
			_callbacks = new FingerprintAuthCallbacks(Success, Error);
			_cancellationSignal = new CancellationSignal();

			_fingerprintManager.Authenticate(_cryptoObjectHelper.BuildCryptoObject(),
											 (int)FingerprintAuthenticationFlags.None,
											 _cancellationSignal,
											 _callbacks,
											 null);
			return true;
		}

		public bool DeviceOwnerAuthenticationCancelationAvailable()
		{
			// TODO: do interface... lepsza nazwa?
			return true;
		}

		public void CancelDeviceOwnerAuthentication()
		{
			// TODO: do interface... lepsza nazwa?
			if (_cancellationSignal != null)
				_cancellationSignal.Cancel();

			_cancellationSignal = null;
			_fingerprintManager = null;
			_callbacks = null;
		}
	}


	/// <summary>
	/// Fingerprint authentication callbacks.
	/// </summary>
	class FingerprintAuthCallbacks : FingerprintManagerCompat.AuthenticationCallback
	{
		static readonly byte[] SECRET_BYTES = { 34, 12, 67, 16, 87, 62, 27, 48, 29 };

		Action _Success;
		Action<string, bool> _Error;

		public FingerprintAuthCallbacks(Action Success, Action<string, bool> Error)
		{
			_Success = Success;
			_Error = Error;
		}

		public override void OnAuthenticationSucceeded(FingerprintManagerCompat.AuthenticationResult result)
		{
			if (result.CryptoObject.Cipher != null)
			{
				try
				{
					// Calling DoFinal on the Cipher ensures that the encryption worked. If not then exception will be threw.
					byte[] doFinalResult = result.CryptoObject.Cipher.DoFinal(SECRET_BYTES);
					_Success();

					Debug.WriteLine($"SecretsManager: FingerprintAuthCallbacks: Fingerprint authentication succeeded, DoFinal results: {Convert.ToBase64String(doFinalResult)}");
				}
				catch (BadPaddingException bpe)
				{
					OnAuthenticationError(98, bpe.ToString());
				}
				catch (IllegalBlockSizeException ibse)
				{
					OnAuthenticationError(99, ibse.ToString());
				}
			}
			else
			{
				_Success();

				Debug.WriteLine($"SecretsManager: FingerprintAuthCallbacks: Fingerprint authentication succeeded.");
			}
		}

		public override void OnAuthenticationHelp(int helpMsgId, ICharSequence helpString)
		{
			Debug.WriteLine($"SecretsManager: FingerprintAuthCallbacks: OnAuthenticationHelp: {helpMsgId}: {helpString.ToString()}");

			_Error(helpString.ToString(), true);
		}

		public override void OnAuthenticationFailed()
		{
			Debug.WriteLine($"SecretsManager: FingerprintAuthCallbacks: OnAuthenticationFailed");

			_Error("Fingerprint scanned but not recognized.", true); // TODO: translation
		}

		public override void OnAuthenticationError(int errMsgId, ICharSequence errString)
		{
			OnAuthenticationError(errMsgId, errString.ToString());
		}

		new void OnAuthenticationError(int errMsgId, string errString)
		{
			Debug.WriteLine($"SecretsManager: FingerprintAuthCallbacks: OnAuthenticationError: {errMsgId}: {errString}");

			if (errMsgId != (int)FingerprintState.ErrorCanceled)
				_Error(errString.ToString(), false);
		}
	}


	/// <summary>
	/// This class encapsulates the creation of a CryptoObject based on a javax.crypto.Cipher.
	/// </summary>
	/// <remarks>Each invocation of BuildCryptoObject will instantiate a new CryptoObject. 
	/// If necessary a key for the cipher will be created.</remarks>
	class CryptoObjectHelper
	{
		static readonly string KEY_NAME = "0948e431-20db-4b07-8635-1ae9fd50bafe";
		static readonly string KEYSTORE_NAME = "AndroidKeyStore";

		static readonly string KEY_ALGORITHM = KeyProperties.KeyAlgorithmAes;
		static readonly string BLOCK_MODE = KeyProperties.BlockModeCbc;
		static readonly string ENCRYPTION_PADDING = KeyProperties.EncryptionPaddingPkcs7;

		static readonly string TRANSFORMATION = KEY_ALGORITHM + "/" + BLOCK_MODE + "/" + ENCRYPTION_PADDING;

		readonly KeyStore _keystore;

		public CryptoObjectHelper()
		{
			_keystore = KeyStore.GetInstance(KEYSTORE_NAME);
			_keystore.Load(null);
		}

		public FingerprintManagerCompat.CryptoObject BuildCryptoObject()
		{
			Cipher cipher = CreateCipher();
			return new FingerprintManagerCompat.CryptoObject(cipher);
		}

		/// <summary>
		/// Creates the cipher.
		/// </summary>
		/// <returns>The cipher.</returns>
		/// <param name="retry">If set to <c>true</c>, recreate the key and try again.</param>
		Cipher CreateCipher(bool retry = true)
		{
			IKey key = GetKey();
			Cipher cipher = Cipher.GetInstance(TRANSFORMATION);
			try
			{
				cipher.Init(CipherMode.EncryptMode, key);
			}
			catch (KeyPermanentlyInvalidatedException e)
			{
				Debug.WriteLine($"SecretsManager: CryptoObjectHelper: The key was invalidated, creating a new key.");

				_keystore.DeleteEntry(KEY_NAME);
				if (retry)
				{
					CreateCipher(false);
				}
				else
				{
					throw new System.Exception("Could not create the cipher for fingerprint authentication.", e);
				}
			}
			return cipher;
		}

		/// <summary>
		/// Will get the key from the Android keystore, creating it if necessary.
		/// </summary>
		IKey GetKey()
		{
			if (!_keystore.IsKeyEntry(KEY_NAME))
			{
				CreateKey();
			}

			IKey secretKey = _keystore.GetKey(KEY_NAME, null);
			return secretKey;
		}

		/// <summary>
		/// Creates the Key for fingerprint authentication.
		/// </summary>
		void CreateKey()
		{
			KeyGenerator keyGen = KeyGenerator.GetInstance(KEY_ALGORITHM, KEYSTORE_NAME);
			KeyGenParameterSpec keyGenSpec =
				new KeyGenParameterSpec.Builder(KEY_NAME, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
					.SetBlockModes(BLOCK_MODE)
					.SetEncryptionPaddings(ENCRYPTION_PADDING)
					.SetUserAuthenticationRequired(true)
					.Build();

			keyGen.Init(keyGenSpec);
			keyGen.GenerateKey();

			Debug.WriteLine($"SecretsManager: CryptoObjectHelper: New key created for fingerprint authentication.");
		}
	}
}

#endif
