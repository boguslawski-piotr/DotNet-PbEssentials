
using System;

namespace pbXSecurity
{
    public interface ICredentialsManager {
		
        // Basic device owner authentication (pin, passkey, biometrics, etc.)

		bool DeviceOwnerAuthenticationAvailable { get; }
        bool DeviceOwnerAuthenticationWithBiometricsAvailable { get; }
        bool AuthenticateDeviceOwner(string Msg, Action Succes, Action<string> Error);
	}

    public partial class CredentialsManager : ICredentialsManager
    {
        public CredentialsManager()
        {
        }
	}
}
