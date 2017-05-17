using System;

namespace pbXSecurity
{
#if __ANDROID__

	public partial class SecretsManager : ISecretsManager
	{

		public bool DeviceOwnerAuthenticationAvailable
		{
            get { return DeviceOwnerAuthenticationWithBiometricsAvailable; }
		}

		public bool DeviceOwnerAuthenticationWithBiometricsAvailable
		{
            get { return false; }
		}

		public bool AuthenticateDeviceOwner(string Msg, Action Success, Action<string> Error)
		{
			return false;
		}
	}

#endif
}
