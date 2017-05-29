using System;
using Xamarin.Forms;

#if __ANDROID__
using Android.Content;
using Android.Runtime;
using Android.Views;
#endif

#if __IOS__
using UIKit;
#endif

namespace pbXForms
{
    public enum DeviceOrientation
    {
        Undefined,
        Landscape,
        Portrait
    }

    static public class DeviceEx
    {
        static public DeviceOrientation Orientation
        {
            get {
				if (Application.Current != null && Application.Current.MainPage != null)
				{
					// Because the app can run on tablets where split view mode is available, 
					// it is safer to calculate orientation based on the size of the main window.
                    // This helps also on desktops :)
					Rectangle b = Application.Current.MainPage.Bounds;
					return b.Width >= b.Height ? DeviceOrientation.Landscape : DeviceOrientation.Portrait;
				}
#if __IOS__
				var currentOrientation = UIApplication.SharedApplication.StatusBarOrientation;

				bool isPortrait =
					currentOrientation == UIInterfaceOrientation.Portrait
					|| currentOrientation == UIInterfaceOrientation.PortraitUpsideDown;

				return isPortrait ? DeviceOrientation.Portrait : DeviceOrientation.Landscape;
#else
#if __ANDROID__
				IWindowManager windowManager = Android.App.Application.Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

				var rotation = windowManager.DefaultDisplay.Rotation;
				bool isLandscape = rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270;

				return isLandscape ? DeviceOrientation.Landscape : DeviceOrientation.Portrait;
#else
#if WINDOWS_UWP
				var orientation = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Orientation;
	            if (orientation == Windows.UI.ViewManagement.ApplicationViewOrientation.Landscape)
					return DeviceOrientation.Landscape;
                return DeviceOrientation.Portrait;
#else
                // macOS
                return DeviceOrientation.Landscape;
#endif
#endif
#endif
            }
        }

        static public bool StatusBarVisible
        {
            get {
#if __IOS__
                return
                    Device.Idiom != TargetIdiom.Phone
                    || DeviceEx.Orientation != DeviceOrientation.Landscape;
#else
#if __ANDROID__ || WINDOWS_UWP
                return Device.Idiom != TargetIdiom.Desktop;
#else
                // macOS
                return false;
#endif
#endif
            }
        }

		static uint _AnimationsLength = 300;
		public static uint AnimationsLength
		{
			get => (uint)((double)_AnimationsLength * (Device.Idiom == TargetIdiom.Tablet ? 1.33 : Device.Idiom == TargetIdiom.Desktop ? 0.77 : 1));
			set => _AnimationsLength = value;
		}


	}
}
