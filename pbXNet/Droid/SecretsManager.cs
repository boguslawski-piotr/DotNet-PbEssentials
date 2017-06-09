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
using Java.Lang;
using Java.Security;
using Javax.Crypto;

namespace pbXNet
{

	public sealed partial class SecretsManager : ISecretsManager
	{
		public void Initialize(object activity)
		{
			_activity = activity as AppCompatActivity;
		}

		bool DOBiometricsAuthenticationAvailable
		{
			get {
				if (_activity != null)
				{
					_fingerprintManager = FingerprintManagerCompat.From(_activity);
					try
					{
						if (_fingerprintManager.IsHardwareDetected && _fingerprintManager.HasEnrolledFingerprints)
						{
							KeyguardManager keyguardManager = (KeyguardManager)_activity.GetSystemService(Android.Content.Context.KeyguardService);
							if (keyguardManager.IsKeyguardSecure)
								return true;
						}
					}
					catch (System.Exception ex)
					{
						Debug.WriteLine($"SecretsManager: DOBiometricsAuthenticationAvailable: exception: {ex}");
					}
				}
				return false;
			}
		}

		public DOAuthentication AvailableDOAuthentication
		{
			get {
				if (DOBiometricsAuthenticationAvailable)
					return DOAuthentication.Fingerprint;
				return DOAuthentication.None;
			}
		}

		Activity _activity;
		CryptoObjectHelper _cryptoObjectHelper;
		FingerprintManagerCompat _fingerprintManager;
		FingerprintAuthCallbacks _authenticatecallbacks;
		CancellationSignal _cancellationSignal;

		public bool StartDOAuthentication(string msg, Action Success, Action<string, bool> ErrorOrHint)
		{
			if (!DOBiometricsAuthenticationAvailable)
				return false;

			Android.Content.PM.Permission permissionResult = ContextCompat.CheckSelfPermission(_activity, Manifest.Permission.UseFingerprint);
			if (permissionResult != Android.Content.PM.Permission.Granted)
			{
				// If the system did not allow the use of fingerprint scanner then we send 
				// a request to show a dialog that allows the user to decide what to do with it.
				//
				// See: https://developer.android.com/training/permissions/requesting.html

				_activity.RequestPermissions(new string[] { Manifest.Permission.UseFingerprint }, 0);
				ErrorOrHint(T.Localized("FingerprintRequestPermissions"), false);
				return true;

			}

			if (_cryptoObjectHelper == null)
				_cryptoObjectHelper = new CryptoObjectHelper();
			_fingerprintManager = FingerprintManagerCompat.From(_activity);
			_authenticatecallbacks = new FingerprintAuthCallbacks(Success, ErrorOrHint);
			_cancellationSignal = new CancellationSignal();

			_fingerprintManager.Authenticate(_cryptoObjectHelper.BuildCryptoObject(),
											 (int)FingerprintAuthenticationFlags.None,
											 _cancellationSignal,
											 _authenticatecallbacks,
											 null);
			return true;
		}

		public bool CanDOAuthenticationBeCanceled()
		{
			return true;
		}

		public void CancelDOAuthentication()
		{
			if (_cancellationSignal != null)
				_cancellationSignal.Cancel();

			_cancellationSignal = null;
			_fingerprintManager = null;
			_authenticatecallbacks = null;
		}
	}


	/// <summary>
	/// Fingerprint authentication callbacks.
	/// </summary>
	class FingerprintAuthCallbacks : FingerprintManagerCompat.AuthenticationCallback
	{
		static readonly byte[] SECRET_BYTES = { 34, 12, 67, 16, 87, 62, 27, 48, 29 };

		Action _Success;
		Action<string, bool> _ErrorOrHint;

		public FingerprintAuthCallbacks(Action Success, Action<string, bool> ErrorOrHint)
		{
			_Success = Success;
			_ErrorOrHint = ErrorOrHint;
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

			_ErrorOrHint(helpString.ToString(), true);
		}

		public override void OnAuthenticationFailed()
		{
			Debug.WriteLine($"SecretsManager: FingerprintAuthCallbacks: OnAuthenticationFailed");

			_ErrorOrHint(T.Localized("FingerprintAuthenticationFailed"), true);
		}

		public override void OnAuthenticationError(int errMsgId, ICharSequence errString)
		{
			OnAuthenticationError(errMsgId, errString.ToString());
		}

		new void OnAuthenticationError(int errMsgId, string errString)
		{
			Debug.WriteLine($"SecretsManager: FingerprintAuthCallbacks: OnAuthenticationError: {errMsgId}: {errString}");

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
