using System;

namespace pbXNet
{
	public sealed partial class SecretsManager : ISecretsManager
	{
		public void _Initialize(object activity)
		{
			throw new NotImplementedException();
		}

		public DOAuthentication _AvailableDOAuthentication
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool _StartDOAuthentication(string msg, Action Success, Action<string, bool> ErrorOrHint)
		{
			throw new NotImplementedException();
		}

		public bool _CanDOAuthenticationBeCanceled()
		{
			throw new NotImplementedException();
		}

		public bool _CancelDOAuthentication()
		{
			throw new NotImplementedException();
		}
	}
}
