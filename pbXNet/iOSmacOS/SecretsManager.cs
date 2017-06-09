#if __UNIFIED__

using System;
using System.Diagnostics;

using LocalAuthentication;
using Foundation;

namespace pbXNet
{
	public sealed partial class SecretsManager : ISecretsManager
	{
		public void Initialize(object param)
		{
		}

		bool DOAuthenticationAvailable
		{
			get {
				var context = new LAContext();
				return context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthentication, out NSError error);
			}
		}

		bool DOBiometricsAuthenticationAvailable
		{
			get {
				var context = new LAContext();
				return context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out NSError error);
			}
		}

		public DOAuthentication AvailableDOAuthentication
		{
			get {
				if (DOBiometricsAuthenticationAvailable)
					return DOAuthentication.Fingerprint;
				if (DOAuthenticationAvailable)
					return DOAuthentication.Password;
				return DOAuthentication.None;
			}
		}

		bool _StartDOAuthentication(LAContext context, LAPolicy policy, string msg, Action Success, Action<string, bool> ErrorOrHint)
		{
			if (context.CanEvaluatePolicy(policy, out NSError error))
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
								_StartDOAuthentication(context, LAPolicy.DeviceOwnerAuthentication, msg, Success, ErrorOrHint);
							}
							else
								ErrorOrHint(_error.ToString(), false);
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

		public bool StartDOAuthentication(string msg, Action Success, Action<string, bool> ErrorOrHint)
		{
			// It seems that the call with parameter LAPolicy.DeviceOwnerAuthentication automatically uses biometrics authentication when it is set in the system settings.
			// TODO: check StartDOAuthentication on a real device(s)

			var context = new LAContext();
			//if (!context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out NSError error))
			return _StartDOAuthentication(context, LAPolicy.DeviceOwnerAuthentication, msg, Success, ErrorOrHint);
			//else
			//    return _AuthenticateDeviceOwner(context, LAPolicy.DeviceOwnerAuthenticationWithBiometrics, msg, Success, Error);
		}

		public bool CanDOAuthenticationBeCanceled()
		{
			return false;
		}

		public void CancelDOAuthentication()
		{
		}

	}
}

#endif
