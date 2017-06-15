using System;

namespace pbXNet
{
	public sealed partial class SecretsManager : ISecretsManager
	{
		public void Initialize(object activity)
		{
			throw new NotImplementedException();
		}

		public DOAuthentication AvailableDOAuthentication
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool StartDOAuthentication(string msg, Action Success, Action<string, bool> ErrorOrHint)
		{
			throw new NotImplementedException();
		}

		public bool CanDOAuthenticationBeCanceled()
		{
			throw new NotImplementedException();
		}

		public bool CancelDOAuthentication()
		{
			throw new NotImplementedException();
		}
	}
}
