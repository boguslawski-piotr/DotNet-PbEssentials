using System;
using System.Threading.Tasks;

namespace pbXSecurity
{
	public enum CKeyLifeTime
	{
		Undefined,
		Infinite,
		WhileAppRunning,
        WhileAppIsOnTop
	};

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
        Task<string> EncryptAsync(string data, byte[] ckey, byte[] iv);
		Task<string> DecryptAsync(string data, byte[] ckey, byte[] iv);
		byte[] GenerateIV();
	}
}
