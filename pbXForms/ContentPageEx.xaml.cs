using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace pbXForms
{
	public partial class ContentPageEx : ContentPage
	{
		public ContentPageEx()
		{
			InitializeComponent();
		}

		//

		protected bool PageCoversStatusBar =
#if __IOS__
			true;
#endif
#if __ANDROID__ || WINDOWS_UWP
			false;
#endif

		public Layout<View> AppBarRow
		{
			get { return _AppBarRow; }
		}

		public IList<View> AppBar
		{
			get { return _AppBarRow.Children; }
		}

		public IList<View> ContentEx
		{
			get { return _ContentRow.Children; }
		}

		public Layout<View> ToolBarRow
		{
			get { return _ToolBarRow; }
		}

		public IList<View> ToolBar
		{
			get { return _ToolBarRow.Children; }
		}

		//

		private double _osa_width = -1;
		private double _osa_height = -1;

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);
			if (this._osa_width != width || this._osa_height != height)
			{
				this._osa_width = width;
				this._osa_height = height;

				bool IsLandscape = (DeviceEx.Orientation == DeviceOrientations.Landscape);

				bool StatusBarVisible = DeviceEx.StatusBarVisible;

				Grid.RowDefinitions[0].Height =
					(IsLandscape ? Metrics.AppBarHeightLandscape : Metrics.AppBarHeightPortrait)
					+ ((StatusBarVisible && PageCoversStatusBar) ? Metrics.StatusBarHeight : 0);

				_AppBarRow.Padding = new Thickness(
					0,
					(StatusBarVisible && PageCoversStatusBar) ? Metrics.StatusBarHeight : 0,
					0,
					0);

				if (ToolBar.Count > 0)
					Grid.RowDefinitions[2].Height = (IsLandscape ? Metrics.ToolBarHeightLandscape : Metrics.ToolBarHeightPortrait);

				OnLayoutFixed();
			}

		}

		protected virtual void OnLayoutFixed()
		{
		}

	}
}
