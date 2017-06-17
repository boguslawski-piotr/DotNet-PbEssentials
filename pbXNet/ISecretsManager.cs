using System;
using System.Threading.Tasks;

namespace pbXNet
{
	public enum DOAuthentication
	{
		None,
		Password,
		Fingerprint,
		Iris,
		Face,
		UserSelection,
	}

	/// <summary>
	/// Cryptographic key life time definitions used 
	/// in <see cref="ISecretsManager.CreateCKey"><c>ISecretsManager.CreateCKey</c></see>.
	/// </summary>
	public enum CKeyLifeTime
	{
		// WARNING: Do not change constants values order.
		// If needed add new value(s) at the end.

		/// <summary>
		/// Ckey should be stored (in a safe way!) and should be available 
		/// throughout the life of the application on device.
		/// </summary>
		Infinite,

		/// <summary>
		///
		/// </summary>
		WhileAppRunning,

		/// <summary>
		///
		/// </summary>
		WhileAppIsOnTop,
	};

	/// <summary>
	/// Secrets manager.
	/// </summary>
	public interface ISecretsManager
	{
		string Id { get; }

		void Initialize(object param);

		// Basic device owner authentication (pin, passkey, biometrics, etc.)

		DOAuthentication AvailableDOAuthentication { get; }

		bool StartDOAuthentication(string msg, Action Succes, Action<string, bool> ErrorOrHint); // string: err/hint message, bool: this is only a hint?
		bool CanDOAuthenticationBeCanceled();
		bool CancelDOAuthentication();

		// Basic authentication based on passwords
		// Implementation should never store any password anywhere in any form

		void AddOrUpdatePassword(string id, IPassword passwd);
		bool PasswordExists(string id);
		void DeletePassword(string id);

		bool ComparePassword(string id, IPassword passwd);

		// Cryptographic keys, encryption and decryption

		void CreateCKey(string id, CKeyLifeTime lifeTime, IPassword passwd);
		bool CKeyExists(string id);
		void DeleteCKey(string id);

		IByteBuffer GenerateIV();

		string Encrypt(string data, string id, IByteBuffer iv);
		string Decrypt(string data, string id, IByteBuffer iv);
	}
}
