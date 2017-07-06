#if WINDOWS_UWP

using System.Collections.Generic;
using System.Text;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
	public static partial class DeviceEx
	{
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