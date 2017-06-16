using System;
using System.IO;
using System.Threading.Tasks;

namespace pbXNet
{
	public partial class AesCryptographer : ICryptographer
	{
		public Exception LastEx { get; private set; }

		// You will find the implementation in the platform directories...
	}
}
