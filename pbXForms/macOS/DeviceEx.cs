#if __MACOS__

using System.Reflection;
using pbXNet;
using Security;
using Xamarin.Forms;

namespace pbXForms
{
	public static partial class DeviceEx
	{
		static string _Id
		{
			get {
				string serviceName = Application.Current != null ? Application.Current.GetType().Assembly.ManifestModule.Name : "pbXForms";
				int firstDot = serviceName.IndexOf('.');
				if (firstDot > 0)
					serviceName = serviceName.Substring(0, firstDot);
				const string accName = "DeviceEx.Id";

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

		static DeviceOrientation _Orientation
		{
			get {
				return DeviceOrientation.Landscape;
			}
		}

		static bool _StatusBarVisible
		{
			get {
				return false;
			}
		}

	}
}

#endif