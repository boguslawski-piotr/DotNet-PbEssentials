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
    public enum DeviceOrientations
    {
        Undefined,
        Landscape,
        Portrait
    }

    static public class DeviceEx
    {
        static public DeviceOrientations Orientation
        {
            get
            {
#if __IOS__
                // TODO: dziala dopiero jak glowne okno jest w pelni zbudowane :( -> poprawic aby dzialalo zawsze
                // TODO: sprawdzic to samo na Android i UWP

                var currentOrientation = UIApplication.SharedApplication.StatusBarOrientation;

				bool isPortrait =
					currentOrientation == UIInterfaceOrientation.Portrait
					|| currentOrientation == UIInterfaceOrientation.PortraitUpsideDown;

				return isPortrait ? DeviceOrientations.Portrait : DeviceOrientations.Landscape;
#else
#if __ANDROID__
				IWindowManager windowManager = Android.App.Application.Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

				var rotation = windowManager.DefaultDisplay.Rotation;
				bool isLandscape = rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270;

				return isLandscape ? DeviceOrientations.Landscape : DeviceOrientations.Portrait;
#else
#if WINDOWS_UWP
				var orientation = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Orientation;
	            if (orientation == Windows.UI.ViewManagement.ApplicationViewOrientation.Landscape)
					return DeviceOrientations.Landscape;
                return DeviceOrientations.Portrait;
#else
                // macOS
                return DeviceOrientations.Landscape;
#endif
#endif
#endif
            }
        }

        static public bool StatusBarVisible
        {
            get
            {
#if __IOS__
				return
					DeviceEx.Orientation != DeviceOrientations.Landscape
					|| Device.Idiom == TargetIdiom.Tablet;
#endif
#if __ANDROID__ || WINDOWS_UWP
				return true;
#else
                // macOS
                return false;
#endif
            }
        }

    }
}
