#if __MACOS__

using Foundation;
using Security;

namespace pbXNet
{
	public static partial class Tools
	{
		static string _Uaqpid
		{
			get {
				string serviceName = NSBundle.MainBundle.BundleIdentifier;
				const string accName = ".9fe677ab04cc4a0892277312415e514e";

				if (SecKeyChain.FindGenericPassword(serviceName, accName, out byte[] bid) != SecStatusCode.Success)
				{
					bid = Tools.CreateGuid().ToByteArray();
					SecStatusCode ssc = SecKeyChain.AddGenericPassword(serviceName, accName, bid);
				}

				string id = bid.ToHexString();
				string id2 = "macOS";

				return id + id2;
			}
		}
	}
}

#endif