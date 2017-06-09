using System.Collections.Generic;
using System.Text;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
	public enum DeviceOrientation
	{
		Undefined,
		Landscape,
		Portrait
	}

	public static partial class DeviceEx
	{
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

				return _Orientation;
			}
		}

		/// <summary>
		/// Gets a value indicating whether status bar is visible on device in actual mode.
		/// </summary>
		/// <value><c>true</c> if status bar visible; otherwise, <c>false</c>.</value>
		public static bool StatusBarVisible
		{
			get {
				return _StatusBarVisible;
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
