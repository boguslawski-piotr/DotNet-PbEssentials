using System;
using System.Collections.Generic;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
    public partial class ContentPageExView : ContentView
    {
        protected Grid _Grid => __Grid;

        //protected Layout<View> AppBarRow => _AppBarRow;
        public IList<View> AppBarContent => _AppBarRow.Children;

        //protected Layout<View> ContentRow => _ContentRow;
        public IList<View> ContentEx => _ContentRow.Children;

        //protected Layout<View> ToolBarRow => _ToolBarRow;
        public IList<View> ToolBarContent => _ToolBarRow.Children;

        public ContentPageExView()
        {
            InitializeComponent();
        }

        public static void LayoutAppBarAndToolBar(double width, double height, Grid Grid, Layout<View> AppBarRow, Layout<View> ToolBarRow)
        {
            bool IsLandscape = (DeviceEx.Orientation == DeviceOrientations.Landscape);

            bool PageCoversStatusBar =
#if __IOS__
                true;
#else
				false;
#endif

            if (AppBarRow.Children?.Count > 0)
            {
                Grid.RowDefinitions[0].Height =
                (IsLandscape ? Metrics.AppBarHeightLandscape : Metrics.AppBarHeightPortrait)
                + ((PageCoversStatusBar) ? Metrics.StatusBarHeight : 0);

                AppBarRow.Padding = new Thickness(
                    0,
                    (PageCoversStatusBar ? Metrics.StatusBarHeight : 0),
                    0,
                    0);
            }

            if (ToolBarRow.Children?.Count > 0)
                Grid.RowDefinitions[2].Height = (IsLandscape ? Metrics.ToolBarHeightLandscape : Metrics.ToolBarHeightPortrait);
        }

        Size _osa;

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (_Grid == null)
                return;
            if (!Tools.IsDifferent(new Size(width, height), ref _osa))
                return;

            BatchBegin();

            LayoutAppBarAndToolBar(width, height, _Grid, _AppBarRow, _ToolBarRow);

            BatchCommit();
        }

    }
}
