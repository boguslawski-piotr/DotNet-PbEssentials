#if __ANDROID__

using System;

namespace pbXSecurity
{

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

}

#endif
