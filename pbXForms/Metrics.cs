using System;
using Xamarin.Forms;

#if WINDOWS_UWP
using Windows.UI.ViewManagement;
#endif

namespace pbXForms
{
	public class Metrics
	{
#if __IOS__
		public static double StatusBarHeight = 20;
#else
#if __ANDROID__
		public static double StatusBarHeight = 24;
#else
#if WINDOWS_UWP
		public static double StatusBarHeight = Device.Idiom == TargetIdiom.Desktop ? 0 : StatusBar.GetForCurrentView().OccludedRect.Height;
#else
		public static double StatusBarHeight = 0;
#endif
#endif
#endif
		public static double ScreenEdgeMargin = 16;
		public static double ScreenEdgeHalfMargin = 8;

		public static Thickness ScreenEdgeLeftRightPadding = new Thickness(ScreenEdgeMargin, 0);
		public static Thickness ScreenEdgeLeftPadding = new Thickness(ScreenEdgeMargin, 0, 0, 0);
		public static Thickness ScreenEdgeRightPadding = new Thickness(0, 0, ScreenEdgeMargin, 0);
		public static Thickness ScreenEdgeLeftRightTopPadding = new Thickness(ScreenEdgeMargin, ScreenEdgeMargin, ScreenEdgeMargin, 0);
		public static Thickness ScreenEdgeLeftRightBottomPadding = new Thickness(ScreenEdgeMargin, 0, ScreenEdgeMargin, ScreenEdgeMargin);

		public static Thickness ScreenEdgeTopBottomPadding = new Thickness(0, ScreenEdgeMargin);
		public static Thickness ScreenEdgeTopPadding = new Thickness(0, ScreenEdgeMargin, 0, 0);
		public static Thickness ScreenEdgeBottomPadding = new Thickness(0, 0, 0, ScreenEdgeMargin);

		public static Thickness ScreenEdgePadding = new Thickness(ScreenEdgeMargin, ScreenEdgeMargin);


		public static double AppBarHeightPortrait = Device.Idiom != TargetIdiom.Phone ? 80 : 56;
		public static double AppBarHeightLandscape = Device.Idiom != TargetIdiom.Phone ? 80 : 48;

		public static double ToolBarHeightPortrait = Device.Idiom != TargetIdiom.Phone ? 64 : 56;
		public static double ToolBarHeightLandscape = Device.Idiom != TargetIdiom.Phone ? 64 : 48;


		public static double ToolBarItemsSpacing = 16;

		public static double ButtonItemsSpacing = 8;

		public static double TouchTargetHeight = 48;

		public static double IconHeight = 24;
		public static double SmallIconHeight = 16;

		public static double ListItemHeight = Device.Idiom != TargetIdiom.Phone ? 64 : 56;
		public static Int32 ListItemHeightInt32 = (Int32)ListItemHeight;

		public static double TallListItemHeight = Device.Idiom != TargetIdiom.Phone ? 80 : 72;
		public static Int32 TallListItemHeightInt32 = (Int32)TallListItemHeight;
	}
}
