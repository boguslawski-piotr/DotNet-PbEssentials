using System;
using System.IO;
using System.Threading.Tasks;

namespace pbXNet
{
	public partial class AesCryptographer : ICryptographer
	{
		public Exception LastEx { get; private set; }

		// Implementation in:
		//
		// UWP: pbXNet\UWP\
		// iOS, macOS, Android, .NET: pbXNet\NETStd2\
	}
}
