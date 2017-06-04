using System.Collections.Generic;
using System.Text;
using pbXNet;
using pbXSecurity;
using Xamarin.Forms;
using System.Diagnostics;

#if __ANDROID__
using Android.Content;
using Android.Runtime;
using Android.Views;
#endif

#if __IOS__
using UIKit;
using Foundation;
#endif

namespace pbXForms
{
	public enum DeviceOrientation
	{
		Undefined,
		Landscape,
		Portrait
	}

	public static class DeviceEx
	{
		/// <summary>
		/// Gets the unique device identifier (should be really unique accross all devices with the same operating system).
		/// </summary>
		public static string Id
		{
			get {
#if __IOS__
				string id = UIDevice.CurrentDevice.IdentifierForVendor.AsString();
				string id2 = UIDevice.CurrentDevice.Model;
#endif
#if __MACOS__
				//Mono.Posix.Syscall ???

				// TODO: DeviceEx.Id for macOS
				string id = "1e08400f-8fe7-4565-acc2-7f8b26f98af4";
				string id2 = "macOS";
#endif
#if __ANDROID__
				string id = Android.Provider.Settings.Secure.AndroidId;
				string id2 = "34535a7e-d8ff-4a45-99de-c8507802b498";
#endif
#if WINDOWS_UWP
                // TODO: DeviceEx.Id for UWP
                string id = "b3fea4b6-0f44-466e-96e0-ba25324671fc";
                string id2 = "UWP";
#endif
				byte[] ckey = new AesCryptographer().GenerateKey(Encoding.UTF8.GetBytes(id + id2), new byte[] { 34, 56, 2, 34, 6, 87, 12, 34, 56, 11 });
				return ConvertEx.ToHexString(ckey);
			}
		}

		/// <summary>
		/// Gets the device/main window (if created) orientation.
		/// </summary>
		public static DeviceOrientation Orientation
		{
			get {
				if (Application.Current != null && Application.Current.MainPage != null)
				{
					// Because the app can run on tablets where split view mode is available, 
					// it is safer to calculate orientation based on the size of the main/top window.
					// This helps also on desktops :)
					Rectangle b = Application.Current.MainPage.Bounds;
					if (Application.Current.MainPage.Navigation?.ModalStack?.Count > 0)
					{
						try
						{
							IEnumerator<Xamarin.Forms.Page> ModalPages = Application.Current.MainPage.Navigation.ModalStack.GetEnumerator();
							while (ModalPages.MoveNext())
								if (!ModalPages.Current.Bounds.IsEmpty)
									b = ModalPages.Current.Bounds;
						}
						catch { }
					}
					return b.Width >= b.Height ? DeviceOrientation.Landscape : DeviceOrientation.Portrait;
				}
#if __IOS__
				var currentDeviceOrientation = UIDevice.CurrentDevice.Orientation;
				if (currentDeviceOrientation != UIDeviceOrientation.Unknown)
				{
					// TODO: czemu UIDevice.CurrentDevice.Orientation nie dziala?
				}

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

		/// <summary>
		/// Gets a value indicating whether status bar is visible on device in actual mode.
		/// </summary>
		/// <value><c>true</c> if status bar visible; otherwise, <c>false</c>.</value>
		public static bool StatusBarVisible
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

		/// <summary>
		/// The default length of the animations for a device or device type/idiom.
		/// </summary>
		public static uint AnimationsLength
		{
			get => (uint)((double)_AnimationsLength * (Device.Idiom == TargetIdiom.Tablet ? 1.25 : Device.Idiom == TargetIdiom.Desktop ? 0.75 : 1));
			set => _AnimationsLength = value;
		}
	}
}
