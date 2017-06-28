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
	public partial class DOAuthentication
	{
		static bool Available
		{
			get {
				var context = new LAContext();
				return context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthentication, out NSError error);
			}
		}

		static bool BiometricsAvailable
		{
			get {
				var context = new LAContext();
				return context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out NSError error);
			}
		}

		static DOAuthenticationType _Type
		{
			get {
				// See: https://developer.apple.com/documentation/localauthentication/lacontext
#if __MACOS__
				var info = new NSProcessInfo(); 
				var minVersion = new NSOperatingSystemVersion(10, 10, 0);
				if (!info.IsOperatingSystemAtLeastVersion(minVersion))
					return DOAuthenticationType.NotAvailable;
#else
				if (!UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
					return DOAuthenticationType.NotAvailable;
#endif
				
				if (BiometricsAvailable)
					return DOAuthenticationType.Fingerprint;
				if (Available)
					return DOAuthenticationType.Password;

				return DOAuthenticationType.NotAvailable;
			}
		}

		static bool _Start(LAContext context, LAPolicy policy, string msg, Action Success, Action<string, bool> ErrorOrHint)
		{
			if (context.CanEvaluatePolicy(policy, out NSError error))
			{
				Log.D($"policy: {policy}");

				var replyHandler = new LAContextReplyHandler((bool success, NSError _error) =>
				{
					context.BeginInvokeOnMainThread(() =>
					{
						if (success)
						{
							Log.I("success");

							Success();
						}
						else
						{
							Log.E(_error.ToString());

							if (_error.Code == Convert.ToInt32(LAStatus.UserFallback)
								&& policy == LAPolicy.DeviceOwnerAuthenticationWithBiometrics)
							{
								_Start(context, LAPolicy.DeviceOwnerAuthentication, msg, Success, ErrorOrHint);
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
				Log.E($"(policy: {policy}), error: {error}");
			}

			return false;
		}

		static bool _Start(string msg, Action Success, Action<string, bool> ErrorOrHint)
		{
			// It seems that the call with parameter LAPolicy.DeviceOwnerAuthentication automatically uses biometrics authentication when it is set in the system settings.
			// TODO: check StartDOAuthentication on a real device(s)

			var context = new LAContext();
			//if (!context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out NSError error))
			return _Start(context, LAPolicy.DeviceOwnerAuthentication, msg, Success, ErrorOrHint);
			//else
			//    return _AuthenticateDeviceOwner(context, LAPolicy.DeviceOwnerAuthenticationWithBiometrics, msg, Success, Error);
		}

		static bool _CanBeCanceled()
		{
			return false;
		}

		static bool _Cancel()
		{
			return false;
		}
	}
}

#endif
