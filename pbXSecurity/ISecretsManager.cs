using System;
using System.Threading.Tasks;

namespace pbXSecurity
{
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
        //

        string Id { get; }


		// Basic device owner authentication (pin, passkey, biometrics, etc.)

		bool DeviceOwnerAuthenticationAvailable { get; }
        bool DeviceOwnerAuthenticationWithBiometricsAvailable { get; }
        bool AuthenticateDeviceOwner(string msg, Action Succes, Action<string> Error);


        // Basic authentication based on passwords
        // Implementation should never store any password anywhere in any form

        Task<bool> PasswordExistsAsync(string id);
        Task AddOrUpdatePasswordAsync(string id, string passwd);
        Task DeletePasswordAsync(string id);
        Task<bool> ComparePasswordAsync(string id, string passwd);


        // Cryptographic keys, encryption and decryption

        Task<byte[]> CreateCKeyAsync(string id, CKeyLifeTime lifeTime, string passwd);
        Task<byte[]> GetCKeyAsync(string id);
        Task DeleteCKeyAsync(string id);

        Task<string> EncryptAsync(string data, byte[] ckey, byte[] iv);
		Task<string> DecryptAsync(string data, byte[] ckey, byte[] iv);
		byte[] GenerateIV();
	}
}
