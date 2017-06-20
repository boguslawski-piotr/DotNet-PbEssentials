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
	/// Secret life time definitions used in
	/// <see cref="ISecretsManager.CreateCKey"/>,
	/// and
	/// <see cref="ISecretsManager.AddOrUpdateSecret{T}"/>.
	/// </summary>
	public enum SecretLifeTime
	{
		// WARNING: Do not change constants values order.
		// If needed add new value(s) at the end.

		Undefined,

		/// <summary>
		/// Secret should be stored (in a safe way!) and should be available 
		/// throughout the life of the application on device.
		/// </summary>
		Infinite,

		/// <summary>
		/// Secret should be available whie app is running.
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

		void AddOrUpdatePassword(string id, SecretLifeTime lifeTime, IPassword passwd);
		bool PasswordExists(string id);
		void DeletePassword(string id);

		bool ComparePassword(string id, IPassword passwd);

		// Cryptographic keys, encryption and decryption

		void CreateCKey(string id, SecretLifeTime lifeTime, IPassword passwd);
		bool CKeyExists(string id);
		void DeleteCKey(string id);

		IByteBuffer GenerateIV();

		string Encrypt(string data, string id, IByteBuffer iv);
		string Decrypt(string data, string id, IByteBuffer iv);

		// Common secrets

		/// <summary>
		/// Adds or updates a secret under the given <paramref name="id"/>.
		/// </summary>
		void AddOrUpdateSecret<T>(string id, SecretLifeTime lifeTime, T data);

		/// <summary>
		/// Checks if a secret with the specified <paramref name="id"/> does exist.
		/// </summary>
		bool SecretExists(string id);

		/// <summary>
		/// Gets a secret with the specified <paramref name="id"/>.
		/// </summary>
		/// <exception cref="System.Collections.Generic.KeyNotFoundException">
		/// Should be thrown when a secret with the specified <c>id</c> does not exist.
		/// </exception>
		T GetSecret<T>(string id);

		/// <summary>
		/// Deletes a secret with the specified <paramref name="id"/>.
		/// </summary>
		void DeleteSecret(string id);
	}
}
