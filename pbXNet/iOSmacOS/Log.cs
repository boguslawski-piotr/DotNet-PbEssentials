using System;
using System.Runtime.InteropServices;

namespace pbXNet
{
	public class NSLogLogger : ILogger
	{
		public void L(DateTime dt, LogType type, string msg)
		{
			msg = $"{type}: {msg}";
			L(msg);
		}

		// Not super elegant solution but it works on iOS Simulator and macOS.
		// Borrowed from this thread: https://forums.xamarin.com/discussion/86226/console-writeline-blank-in-device-log

		// TODO: zobaczyc czy dziala na prawdziwym urzadzeniu

		// TODO: uzyc nowego systemu dostepnego od iOS 10/mac OS
		// https://developer.apple.com/documentation/os/logging#1682426

		const string FoundationLibrary = "/System/Library/Frameworks/Foundation.framework/Foundation";

		[System.Runtime.InteropServices.DllImport(FoundationLibrary)]
		extern static void NSLog(IntPtr format, IntPtr s);

//		[System.Runtime.InteropServices.DllImport(FoundationLibrary, EntryPoint = "NSLog")]
//		extern static void NSLog_ARM64(IntPtr format, IntPtr p2, IntPtr p3, IntPtr p4, IntPtr p5, IntPtr p6, IntPtr p7, IntPtr p8, [MarshalAs(UnmanagedType.LPStr)] string s);

//		static readonly bool Is64Bit = IntPtr.Size == 8;

//#if __MOBILE__
//		static readonly bool IsDevice = true;
//#else
//		static readonly bool IsDevice = false;
//#endif

		static readonly Foundation.NSString nsFormat = new Foundation.NSString(@"%@");

		static void L(string text)
		{
			using (var nsText = new Foundation.NSString(text))
			{
				//if (IsDevice && Is64Bit)
				//{
				//	NSLog_ARM64(nsFormat.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, text);
				//}
				//else
				{
					NSLog(nsFormat.Handle, nsText.Handle);
				}
			}
		}
	}
}
