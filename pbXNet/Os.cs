using System;
using System.Collections.Generic;
using System.Text;

namespace pbXNet
{
    public static class Os
    {
#if !WINDOWS_UWP && !NETSTANDARD1_6
		static Lazy<OperatingSystem> _current = new Lazy<OperatingSystem>(() => Environment.OSVersion);
		static OperatingSystem Current => _current.Value;

		public static bool IsWindows => Current.Platform != PlatformID.MacOSX && Current.Platform != PlatformID.Unix;
		public static bool IsMacOS => Current.Platform == PlatformID.MacOSX;
		public static bool IsUnix => Current.Platform == PlatformID.Unix;
#endif
#if WINDOWS_UWP
		public static bool IsWindows => true;
		public static bool IsMacOS => false;
		public static bool IsUnix => false;
#endif
	}
}
