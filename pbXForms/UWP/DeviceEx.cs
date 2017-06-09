#if WINDOWS_UWP

using System.Collections.Generic;
using System.Text;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
	public static partial class DeviceEx
	{
		static string _Id
		{
			get {
				// TODO: DeviceEx.Id for UWP
				string id = "b3fea4b6-0f44-466e-96e0-ba25324671fc";
				string id2 = "UWP";
				return id + id2;
			}
		}

		static DeviceOrientation _Orientation
		{
			get {
				var orientation = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Orientation;
				if (orientation == Windows.UI.ViewManagement.ApplicationViewOrientation.Landscape)
					return DeviceOrientation.Landscape;
				return DeviceOrientation.Portrait;
			}
		}

		static bool _StatusBarVisible
		{
			get {
				return Device.Idiom != TargetIdiom.Desktop;
			}
		}
	}
}

#endif