#if __ANDROID__

using System;
using Android;
using Android.App;
using Android.Content;
using Android.Hardware.Fingerprints;
using Android.Security.Keystore;
using Android.Support.V4.Content;
using Android.Support.V4.Hardware.Fingerprint;
using Android.Support.V4.OS;
using Android.Views;
using Java.Lang;
using Java.Security;
using Javax.Crypto;

namespace pbXNet
{
	public static partial class DOAuthentication
	{
		public static Activity MainActivity;

		static bool Available
		{
			get {
				if (MainActivity != null && Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
				{
					try
					{
						KeyguardManager keyguardManager = (KeyguardManager)MainActivity.GetSystemService(Android.Content.Context.KeyguardService);
						if (keyguardManager != null)
							return keyguardManager.IsKeyguardSecure;
					}
					catch (System.Exception ex)
					{
						Log.E(ex.Message);
					}
				}
				return false;
			}
		}

		static bool BiometricsAvailable
		{
			get {
				if (MainActivity != null && Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
				{
					try
					{
						_fingerprintManager = FingerprintManagerCompat.From(MainActivity);
						if (_fingerprintManager.IsHardwareDetected && _fingerprintManager.HasEnrolledFingerprints)
						{
							return Available;
						}
					}
					catch (System.Exception ex)
					{
						Log.E(ex.Message);
					}
				}
				return false;
			}
		}

		static DOAuthenticationType _Type
		{
			get {
				if (BiometricsAvailable)
					return DOAuthenticationType.Fingerprint;
				if (Available)
					return DOAuthenticationType.UserSelection;
				return DOAuthenticationType.NotAvailable;
			}
		}

		static bool _Start(string msg, Action Success, Action<string, bool> ErrorOrHint)
		{
			if (BiometricsAvailable)
				return __StartBiometrics(msg, Success, ErrorOrHint);

			if (Available)
				return __Start(msg, Success, ErrorOrHint);

			return false;
		}

		static AuthenticationCallbacks _authenticationCallbacks;

		const int REQUEST_CODE_CONFIRM_DEVICE_CREDENTIALS = 63213;

		static bool __Start(string msg, Action Success, Action<string, bool> ErrorOrHint)
		{
			if (MainActivity != null)
			{
				try
				{
					KeyguardManager keyguardManager = (KeyguardManager)MainActivity.GetSystemService(Android.Content.Context.KeyguardService);
					if (keyguardManager != null && keyguardManager.IsKeyguardSecure)
					{
						Intent intent = keyguardManager.CreateConfirmDeviceCredentialIntent((string)null, msg);
						if (intent != null)
						{
							_authenticationCallbacks = new AuthenticationCallbacks(Success, ErrorOrHint);
							_cancellationSignal = null;
							MainActivity.StartActivityForResult(intent, REQUEST_CODE_CONFIRM_DEVICE_CREDENTIALS);
							return true;
						}
					}
				}
				catch (System.Exception ex)
				{
					Log.E(ex.Message);
				}
			}
			return false;
		}

		public static bool OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			if (_authenticationCallbacks == null || requestCode != REQUEST_CODE_CONFIRM_DEVICE_CREDENTIALS)
				return false;

			_authenticationCallbacks.OnAuthenticationFinished(resultCode);
			_authenticationCallbacks = null;

			return true;
		}

		static FingerprintManagerCompat _fingerprintManager;
		static CancellationSignal _cancellationSignal;
		static CryptoObjectHelper _cryptoObjectHelper;

		static bool __StartBiometrics(string msg, Action Success, Action<string, bool> ErrorOrHint)
		{
			try
			{
				Android.Content.PM.Permission permissionResult = ContextCompat.CheckSelfPermission(MainActivity, Manifest.Permission.UseFingerprint);
				if (permissionResult != Android.Content.PM.Permission.Granted)
				{
					// If the system did not allow the use of fingerprint scanner then we send 
					// a request to show a dialog that allows the user to decide what to do with it.
					//
					// See: https://developer.android.com/training/permissions/requesting.html

					MainActivity.RequestPermissions(new string[] { Manifest.Permission.UseFingerprint }, 0);
					ErrorOrHint(T.Localized("FingerprintRequestPermissions"), false);

					return true;
				}

				if (_cryptoObjectHelper == null)
					_cryptoObjectHelper = new CryptoObjectHelper();

				_fingerprintManager = FingerprintManagerCompat.From(MainActivity);
				_authenticationCallbacks = new AuthenticationCallbacks(Success, ErrorOrHint);
				_cancellationSignal = new CancellationSignal();

				_fingerprintManager.Authenticate(_cryptoObjectHelper.BuildCryptoObject(),
												 (int)FingerprintAuthenticationFlags.None,
												 _cancellationSignal,
												 _authenticationCallbacks,
												 null);
				return true;
			}
			catch (System.Exception ex)
			{
				Log.E(ex.Message);
			}
			return false;
		}

		static bool _CanBeCanceled()
		{
			return _cancellationSignal != null;
		}

		static bool _Cancel()
		{
			bool rc = false;

			if (_cancellationSignal != null)
			{
				_cancellationSignal.Cancel();
				rc = true;
			}

			_cancellationSignal = null;
			_fingerprintManager = null;
			_authenticationCallbacks = null;

			return rc;
		}
	}


	/// <summary>
	/// Authentication callbacks.
	/// </summary>
	class AuthenticationCallbacks : FingerprintManagerCompat.AuthenticationCallback
	{
		Action _Success;
		Action<string, bool> _ErrorOrHint;

		public AuthenticationCallbacks(Action Success, Action<string, bool> ErrorOrHint)
		{
			_Success = Success;
			_ErrorOrHint = ErrorOrHint;
		}

		public void OnAuthenticationFinished(Result resultCode)
		{
			if (resultCode == Result.Ok)
			{
				_Success();
			}
			else
			{
				OnAuthenticationError(100, T.Localized("AuthenticationFailed"));
			}
		}

		public override void OnAuthenticationSucceeded(FingerprintManagerCompat.AuthenticationResult result)
		{
			if (result.CryptoObject.Cipher != null)
			{
				try
				{
					// Calling DoFinal on the Cipher ensures that the encryption worked. If not then exception will be threw.
					byte[] _secret = { 34, 12, 67, 16, 87, 62, 27, 48, 29 };
					byte[] doFinalResult = result.CryptoObject.Cipher.DoFinal(_secret);
					_Success();

					Log.I($"DoFinal results: {Convert.ToBase64String(doFinalResult)}", this);
				}
				catch (BadPaddingException e)
				{
					OnAuthenticationError(101, e.Message);
				}
				catch (IllegalBlockSizeException e)
				{
					OnAuthenticationError(102, e.Message);
				}
				catch (Java.Lang.Exception e)
				{
					OnAuthenticationError(103, e.Cause.Message);
				}
				catch (System.Exception e)
				{
					OnAuthenticationError(104, e.Message);
				}
			}
			else
			{
				_Success();

				Log.I("", this);
			}
		}

		public override void OnAuthenticationHelp(int helpMsgId, ICharSequence helpString)
		{
			Log.D($"{helpMsgId}: {helpString.ToString()}", this);

			_ErrorOrHint(helpString.ToString(), true);
		}

		public override void OnAuthenticationFailed()
		{
			Log.E("", this);

			_ErrorOrHint(T.Localized("FingerprintAuthenticationFailed"), true);
		}

		public override void OnAuthenticationError(int errMsgId, ICharSequence errString)
		{
			OnAuthenticationError(errMsgId, errString.ToString());
		}

		new void OnAuthenticationError(int errMsgId, string errString)
		{
			Log.E($"{errMsgId}: {errString}", this);

			if (errMsgId != (int)FingerprintState.ErrorCanceled)
				_ErrorOrHint(errString.ToString(), false);
		}
	}


	/// <summary>
	/// This class encapsulates the creation of a CryptoObject based on a javax.crypto.Cipher.
	/// </summary>
	/// <remarks>Each invocation of BuildCryptoObject will instantiate a new CryptoObject. 
	/// If necessary a key for the cipher will be created.</remarks>
	class CryptoObjectHelper
	{
		static readonly string KEY_NAME = ".a2a60ff0a53c4ae3a8465c4dfac16ba4";
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
				Log.I($"the key was invalidated, creating a new key", this);

				_keystore.DeleteEntry(KEY_NAME);
				if (retry)
				{
					CreateCipher(false);
				}
				else
				{
					Log.E(e.Message, this);
					throw new System.Exception("Could not create the cipher for authentication.", e);
				}
			}
			return cipher;
		}

		/// <summary>
		/// Will get the Key from the Android keystore, creating it if necessary.
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
		/// Creates the Key from the Android keystore for authentication.
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

			Log.I($"new key created for authentication", this);
		}
	}
}

#endif
