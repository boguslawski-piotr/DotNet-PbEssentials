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
	/// Cryptographic keys life time definitions 
	/// used in ISecretsManager.CreateCKeyAsync.
	/// WARNING: Do not change constants values order.
	/// If needed add new value(s) at the end.
	/// </summary>
	public enum CKeyLifeTime
	{
		Undefined,
		Infinite,
		WhileAppRunning,
		WhileAppIsOnTop,
		OneTime
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

		byte[] CreateCKey(string id, CKeyLifeTime lifeTime, IPassword passwd);
		byte[] GetCKey(string id);
		void DeleteCKey(string id);

		string Encrypt(string data, byte[] ckey, byte[] iv);
		string Decrypt(string data, byte[] ckey, byte[] iv);

		byte[] GenerateIV();
	}
}
