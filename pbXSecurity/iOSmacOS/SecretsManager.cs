#if __UNIFIED__ || __IOS__

using System;
using System.Diagnostics;

using LocalAuthentication;
using Foundation;
using System.Text;

namespace pbXSecurity
{
	public partial class SecretsManager : ISecretsManager
	{
		public void Initialize(object param)
		{
		}

		public bool DeviceOwnerAuthenticationAvailable
		{
			get {
				var context = new LAContext();
				return context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthentication, out NSError error);
			}
		}

		public bool DeviceOwnerAuthenticationWithBiometricsAvailable
		{
			get {
				var context = new LAContext();
				return context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out NSError error);
			}
		}

		bool _AuthenticateDeviceOwner(LAContext context, LAPolicy policy, string msg, Action Success, Action<string, bool> Error)
		{
			NSError error;
			if (context.CanEvaluatePolicy(policy, out error))
			{
				Debug.WriteLine($"SecretsManager: _AuthenticateDeviceOwner: policy: {policy}");

				var replyHandler = new LAContextReplyHandler((bool success, NSError _error) =>
				{
					context.BeginInvokeOnMainThread(() =>
					{
						if (success)
						{
							Debug.WriteLine($"SecretsManager: _AuthenticateDeviceOwner: success");

							Success();
						}
						else
						{
							Debug.WriteLine($"SecretsManager: _AuthenticateDeviceOwner: error: {_error}");

							if (_error.Code == Convert.ToInt32(LAStatus.UserFallback)
								&& policy == LAPolicy.DeviceOwnerAuthenticationWithBiometrics)
							{
								_AuthenticateDeviceOwner(context, LAPolicy.DeviceOwnerAuthentication, msg, Success, Error);
							}
							else
								Error(_error.ToString(), false);
						}
					});

				});

				context.EvaluatePolicy(policy, new NSString(msg), replyHandler);

				return true;
			}
			else
			{
				Debug.WriteLine($"SecretsManager: _AuthenticateDeviceOwner: (policy: {policy}), error: {error}");
			}

			return false;
		}

		public bool AuthenticateDeviceOwner(string msg, Action Success, Action<string, bool> Error)
		{
			// It seems that the call with parameter LAPolicy.DeviceOwnerAuthentication automatically uses biometrics authentication when it is set in the system settings.
			// TODO: check AuthenticateDeviceOwner on a real device(s)

			var context = new LAContext();
			//if (!context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out NSError error))
			return _AuthenticateDeviceOwner(context, LAPolicy.DeviceOwnerAuthentication, msg, Success, Error);
			//else
			//    return _AuthenticateDeviceOwner(context, LAPolicy.DeviceOwnerAuthenticationWithBiometrics, msg, Success, Error);
		}
	}
}

#endif
