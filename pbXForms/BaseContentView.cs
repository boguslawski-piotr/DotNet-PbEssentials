using System;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
	public class BaseContentView : Xamarin.Forms.ContentView
	{
		/// Gets a value indicating whether this view covers status bar.
		public virtual bool ViewCoversStatusBar => Device.RuntimePlatform == Device.iOS && DeviceEx.StatusBarVisible;

		protected virtual Grid ContentLayout { get; } = null;
		protected virtual AppBarLayout AppBarLayout { get; } = null;
		protected virtual Layout<View> ViewLayout { get; } = null;
		protected virtual ToolBarLayout ToolBarLayout { get; } = null;

		Size _osa;

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);

			if (!Tools.MakeIdenticalIfDifferent(new Size(width, height), ref _osa))
				return;
			if (width < 0 && height < 0)
				return;
			
			BatchBegin();

			LayoutAppBarAndToolBar(width, height);

			ContinueOnSizeAllocated(width, height);

			BatchCommit();
		}

		protected virtual (double Portrait, double Landscape) AppBarHeight => (Metrics.AppBarHeightPortrait, Metrics.AppBarHeightLandscape);
		protected virtual (double Portrait, double Landscape) ToolBarHeight => (Metrics.ToolBarHeightPortrait, Metrics.ToolBarHeightLandscape);

		protected virtual void LayoutAppBarAndToolBar(double width, double height)
		{
			if (ContentLayout == null)
				return;

			bool IsLandscape = (DeviceEx.Orientation == DeviceOrientation.Landscape);

			if (AppBarLayout?.Children?.Count > 0)
			{
				ContentLayout.RowDefinitions[0].Height =
					(IsLandscape ? AppBarHeight.Landscape : AppBarHeight.Portrait)
					+ (ViewCoversStatusBar ? Metrics.StatusBarHeight : 0);

				AppBarLayout.Padding = new Thickness(
					0,
					(ViewCoversStatusBar ? Metrics.StatusBarHeight : 0),
					0,
					0);
			}

			if (ToolBarLayout?.Children?.Count > 0)
				ContentLayout.RowDefinitions[2].Height = (IsLandscape ? ToolBarHeight.Landscape : ToolBarHeight.Portrait);
		}

		protected virtual void ContinueOnSizeAllocated(double width, double height)
		{
		}
	}
}
