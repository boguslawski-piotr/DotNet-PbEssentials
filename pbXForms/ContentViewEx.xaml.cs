using System;
using System.Collections.Generic;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{

    public class ContentViewExLayout : Grid
    {
        public ContentViewExLayout()
        {
            Padding = new Thickness(0);
            Margin = new Thickness(0);
            ColumnSpacing = 0;
            RowSpacing = 0;

            ColumnDefinitions = new ColumnDefinitionCollection() {
                    new ColumnDefinition() {
                        Width = GridLength.Star,
                    },
                };

            RowDefinitions = new RowDefinitionCollection() {
                    new RowDefinition() {
                        Height = new GridLength(0)
                    },
                    new RowDefinition() {
                        Height = GridLength.Star
                    },
                    new RowDefinition() {
                        Height = new GridLength(0)
                    },
                };
        }
    }

    public class ContentViewExContentLayout : StackLayout
    {
        public ContentViewExContentLayout()
        {
            Orientation = StackOrientation.Vertical;
            VerticalOptions = LayoutOptions.FillAndExpand;
            HorizontalOptions = LayoutOptions.FillAndExpand;
            Padding = new Thickness(0);
            Margin = new Thickness(0);
            Spacing = 0;
        }
    }


    public partial class ContentViewEx : ContentView
    {
        public AppBarLayout AppBar => _AppBarRow;
        public IList<View> AppBarContent => _AppBarRow.Children;
        public Color AppBarBackgroundColor
        {
            get { return AppBar.BackgroundColor; }
            set { AppBar.BackgroundColor = value; }
        }

        //protected ContentViewExContentLayout View => __ContentRow;
        public IList<View> ViewContent => _ContentRow.Children;

        public ToolBarLayout ToolBar => _ToolBarRow;
        public IList<View> ToolBarContent => _ToolBarRow.Children;
        public Color ToolBarBackgroundColor
        {
            get { return ToolBar.BackgroundColor; }
            set { ToolBar.BackgroundColor = value; }
        }

        public virtual bool ViewCoversStatusBar
        {
            get {
                return
#if __IOS__
                true;
#else
				false;
#endif
            }
        }


        public ContentViewEx()
        {
            InitializeComponent();
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

            LayoutAppBarAndToolBar(width, height);

            ContinueOnSizeAllocated(width, height);

            BatchCommit();
        }

        protected virtual void LayoutAppBarAndToolBar(double width, double height)
        {
            if (_Grid == null)
                return;

            bool IsLandscape = (DeviceEx.Orientation == DeviceOrientation.Landscape);

            if (_AppBarRow?.Children?.Count > 0)
            {
                _Grid.RowDefinitions[0].Height =
                    (IsLandscape ? Metrics.AppBarHeightLandscape : Metrics.AppBarHeightPortrait)
                    + ((ViewCoversStatusBar && DeviceEx.StatusBarVisible) ? Metrics.StatusBarHeight : 0);

                _AppBarRow.Padding = new Thickness(
                    0,
                    (ViewCoversStatusBar && DeviceEx.StatusBarVisible ? Metrics.StatusBarHeight : 0),
                    0,
                    0);
            }

            if (_ToolBarRow?.Children?.Count > 0)
                _Grid.RowDefinitions[2].Height = (IsLandscape ? Metrics.ToolBarHeightLandscape : Metrics.ToolBarHeightPortrait);
        }

        protected virtual void ContinueOnSizeAllocated(double width, double height)
        {
        }
    }
}
