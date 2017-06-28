using System;
namespace pbXNet
{
	public enum DOAuthenticationType
	{
		NotAvailable,
		Password,
		Fingerprint,
		Iris,
		Face,
		UserSelection,
	}

	/// <summary>
	/// Basic device owner authentication (pin, passkey, biometrics, etc.).
	/// </summary>
	public static partial class DOAuthentication
	{
		// You will find the implementation in the platform directories...

		public static DOAuthenticationType Type => _Type;

		public static bool Start(string msg, Action Succes, Action<string, bool> ErrorOrHint) => _Start(msg, Succes, ErrorOrHint);

		public static bool CanBeCanceled() => _CanBeCanceled();

		public static bool Cancel() => _Cancel();
	}
}
