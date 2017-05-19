using System;
using System.Threading.Tasks;

namespace pbXSecurity
{
	public interface ISecretsManager
	{
		// Basic device owner authentication (pin, passkey, biometrics, etc.)

		bool DeviceOwnerAuthenticationAvailable { get; }
		bool DeviceOwnerAuthenticationWithBiometricsAvailable { get; }
		bool AuthenticateDeviceOwner(string Msg, Action Succes, Action<string> Error);

		// Basic authentication based on passwords
		// Implementation should never store any password anywhere in any form

		Task<bool> PasswordExistsAsync(string id);
		Task AddOrUpdatePasswordAsync(string id, string passwd);
		Task DeletePasswordAsync(string id);
		Task<bool> ComparePasswordAsync(string id, string passwd);
	}
}
