#if WINDOWS_UWP

using System;

namespace pbXNet
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

		public bool AuthenticateDeviceOwner(string msg, Action Success, Action<string> Error)
		{
			return false;
		}
	}

}

#endif
