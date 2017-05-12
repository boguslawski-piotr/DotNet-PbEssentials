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

		protected bool AppBarCoversStatusBar = false;

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
		
				bool IsLandscape = (Tools.DeviceOrientation == DeviceOrientations.Landscape);

#if __IOS__
				bool StatusBarVisible = !IsLandscape || Device.Idiom == TargetIdiom.Tablet;
#endif
#if __ANDROID__
				bool StatusBarVisible = AppBarCoversStatusBar;
#endif

				Grid.RowDefinitions[0].Height = (IsLandscape ? Metrics.AppBarHeightLandscape : Metrics.AppBarHeightPortrait) + (StatusBarVisible ? Metrics.StatusBarHeight : 0);

				_AppBarRow.Padding = new Thickness(
					0,
					StatusBarVisible ? Metrics.StatusBarHeight : 0,
					0,
					0);

				if (ToolBar.Count > 0)
					Grid.RowDefinitions[2].Height = (IsLandscape ? Metrics.ToolBarHeightLandscape : Metrics.ToolBarHeightPortrait);
			}

		}

	}
}
