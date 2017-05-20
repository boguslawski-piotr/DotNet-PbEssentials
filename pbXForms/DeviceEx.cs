using System;

#if __ANDROID__
using Android.Content;
using Android.Runtime;
using Android.Views;
#endif

#if __IOS__
using UIKit;
using Xamarin.Forms;
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
#if __IOS__
                // TODO: dziala dopiero jak glowne okno jest w pelni zbudowane :( -> poprawic aby dzialalo zawsze
                // sprawdzic to samo na Android i UWP

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
                    DeviceEx.Orientation != DeviceOrientation.Landscape
                    || Device.Idiom == TargetIdiom.Tablet;
#else
#if __ANDROID__ || WINDOWS_UWP
				return true;
#else
                // macOS
                return false;
#endif
#endif
            }
        }

    }
}
