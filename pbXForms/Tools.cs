using System;

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
	public enum DeviceOrientations
	{
		Undefined,
		Landscape,
		Portrait
	}

	static public class Tools
	{
		static public DeviceOrientations DeviceOrientation
		{
			get
			{
#if __IOS__
				var currentOrientation = UIApplication.SharedApplication.StatusBarOrientation;

				bool isPortrait =
					currentOrientation == UIInterfaceOrientation.Portrait
					|| currentOrientation == UIInterfaceOrientation.PortraitUpsideDown;

				return isPortrait ? DeviceOrientations.Portrait : DeviceOrientations.Landscape;
#endif
#if __ANDROID__
				IWindowManager windowManager = Android.App.Application.Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

				var rotation = windowManager.DefaultDisplay.Rotation;
				bool isLandscape = rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270;
			            
				return isLandscape ? DeviceOrientations.Landscape : DeviceOrientations.Portrait;
#endif
			}
		}
	}
}
