#if __UNIFIED__

using System;
using System.Diagnostics;

using LocalAuthentication;
using Foundation;

#if __IOS__
using UIKit;
#endif

namespace pbXNet
{
	public sealed partial class SecretsManager : ISecretsManager
	{
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

		DOAuthentication _AvailableDOAuthentication
		{
			get {
				// See: https://developer.apple.com/documentation/localauthentication/lacontext
#if __MACOS__
				var info = new NSProcessInfo(); 
				var minVersion = new NSOperatingSystemVersion(10, 10, 0);
				if (!info.IsOperatingSystemAtLeastVersion(minVersion))
					return DOAuthentication.None;
#else
				if (!UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
					return DOAuthentication.None;
#endif
				
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
				Log.D($"policy: {policy}", this);

				var replyHandler = new LAContextReplyHandler((bool success, NSError _error) =>
				{
					context.BeginInvokeOnMainThread(() =>
					{
						if (success)
						{
							Log.I("success", this);

							Success();
						}
						else
						{
							Log.E(_error.ToString(), this);

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
				Log.E($"(policy: {policy}), error: {error}", this);
			}

			return false;
		}

		bool _StartDOAuthentication(string msg, Action Success, Action<string, bool> ErrorOrHint)
		{
			// It seems that the call with parameter LAPolicy.DeviceOwnerAuthentication automatically uses biometrics authentication when it is set in the system settings.
			// TODO: check StartDOAuthentication on a real device(s)

			var context = new LAContext();
			//if (!context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out NSError error))
			return _StartDOAuthentication(context, LAPolicy.DeviceOwnerAuthentication, msg, Success, ErrorOrHint);
			//else
			//    return _AuthenticateDeviceOwner(context, LAPolicy.DeviceOwnerAuthenticationWithBiometrics, msg, Success, Error);
		}

		bool _CanDOAuthenticationBeCanceled()
		{
			return false;
		}

		bool _CancelDOAuthentication()
		{
			return false;
		}

	}
}

#endif
