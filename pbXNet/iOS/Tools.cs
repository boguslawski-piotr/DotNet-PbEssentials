#if __IOS__

using UIKit;

namespace pbXNet
{
	public static partial class Tools
	{
		static string _Uaqpid
		{
			get {
				string id = UIDevice.CurrentDevice?.IdentifierForVendor?.AsString();
				string id2 = UIDevice.CurrentDevice?.Model;
				return id + id2;
			}
		}
	}
}

#endif
