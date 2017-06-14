using System;

namespace pbXNet
{
	public partial class SecretsManager : ISecretsManager
	{
		public void Initialize(object param)
		{
		}

		public DOAuthentication AvailableDOAuthentication => DOAuthentication.None;

		public bool StartDOAuthentication(string msg, Action Succes, Action<string, bool> ErrorOrHint)
		{
			return false;
		}

		public bool CanDOAuthenticationBeCanceled()
		{
			return false;
		}

		public bool CancelDOAuthentication()
		{
			return false;
		}
	}
}
