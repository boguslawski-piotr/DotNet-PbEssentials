
using System;

namespace pbXSecurity
{
    public interface ISecretsManager {
		
        // Basic device owner authentication (pin, passkey, biometrics, etc.)

		bool DeviceOwnerAuthenticationAvailable { get; }
        bool DeviceOwnerAuthenticationWithBiometricsAvailable { get; }
        bool AuthenticateDeviceOwner(string Msg, Action Succes, Action<string> Error);
	}

    public partial class SecretsManager : ISecretsManager
    {
        public SecretsManager()
        {
        }
	}
}
