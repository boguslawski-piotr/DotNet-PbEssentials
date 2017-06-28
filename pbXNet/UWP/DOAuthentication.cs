#if WINDOWS_UWP

using System;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;

namespace pbXNet
{
	public static partial class DOAuthentication
	{
		// Documentation:
		// https://docs.microsoft.com/en-us/uwp/api/windows.security.credentials.ui.userconsentverifier

		static async Task<DOAuthenticationType> CheckConsentAvailabilityAsync()
		{
			try
			{
				// Check the availability of Windows Hello authentication.
				UserConsentVerifierAvailability ucvAvailability = await UserConsentVerifier.CheckAvailabilityAsync();
				return (ucvAvailability == UserConsentVerifierAvailability.Available) ? DOAuthenticationType.UserSelection : DOAuthenticationType.NotAvailable;
			}
			catch (Exception ex)
			{
				Log.E("failed: " + ex.ToString());
			}

			return DOAuthenticationType.NotAvailable;
		}

		static DOAuthenticationType _Type
		{
			get {
				// TODO: this is ugly, try to find better solution
				DOAuthenticationType rc = DOAuthenticationType.NotAvailable;
				Task.Run(async () => rc = await CheckConsentAvailabilityAsync()).GetAwaiter().GetResult();
				return rc;
			}
		}

		static async Task RequestConsentAsync(string userMessage, Action Succes, Action<string, bool> ErrorOrHint)
		{
			string errorMessage = null;
			try
			{
				// Request the logged on user's consent via Windows Hello.
				var consentResult = await UserConsentVerifier.RequestVerificationAsync(userMessage);
				switch (consentResult)
				{
					case UserConsentVerificationResult.Verified:
						Log.I("success");
						Succes();
						break;
					case UserConsentVerificationResult.DeviceBusy:
						errorMessage = T.Localized("BDBusy");
						break;
					case UserConsentVerificationResult.DeviceNotPresent:
						errorMessage = T.Localized("BDNotFound");
						break;
					case UserConsentVerificationResult.DisabledByPolicy:
						errorMessage = T.Localized("BVDisabledByPolicy");
						break;
					case UserConsentVerificationResult.NotConfiguredForUser:
						errorMessage = T.Localized("BVNotConfigured");
						break;
					case UserConsentVerificationResult.RetriesExhausted:
						errorMessage = T.Localized("BVTooManyAttemts");
						break;
					case UserConsentVerificationResult.Canceled:
						errorMessage = T.Localized("BVCanceled");
						break;
					default:
						errorMessage = T.Localized("BVUnavailable");
						break;
				}
			}
			catch (Exception ex)
			{
				errorMessage = T.Localized("BVError") + ex.ToString();
			}

			if(errorMessage != null)
			{
				Log.E(errorMessage);
				ErrorOrHint(errorMessage, false);
			}
		}

		static bool _Start(string msg, Action Succes, Action<string, bool> ErrorOrHint)
		{
			if(_Type != DOAuthenticationType.NotAvailable)
			{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				RequestConsentAsync(msg, Succes, ErrorOrHint);
#pragma warning restore CS4014
				return true;
			}

			return false;
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
