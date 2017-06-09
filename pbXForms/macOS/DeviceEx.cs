#if __MACOS__

namespace pbXForms
{
	public static partial class DeviceEx
	{
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