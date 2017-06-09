#if __ANDROID__

using Android.Content;
using Android.Runtime;
using Android.Views;
using Xamarin.Forms;

namespace pbXForms
{
	public static partial class DeviceEx
	{
		static DeviceOrientation _Orientation
		{
			get {
				IWindowManager windowManager = Android.App.Application.Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

				var rotation = windowManager.DefaultDisplay.Rotation;
				bool isLandscape = rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270;

				return isLandscape ? DeviceOrientation.Landscape : DeviceOrientation.Portrait;
			}
		}

		static bool _StatusBarVisible => Device.Idiom != TargetIdiom.Desktop;
	}
}

#endif