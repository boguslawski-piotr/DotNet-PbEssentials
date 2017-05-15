using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using pbXNet;

namespace pbXForms
{
    public class ContentPageExGrid : Grid
    {
        public ContentPageExGrid()
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

    public class ContentPageExToolBar : StackLayout
    {
        public ContentPageExToolBar()
        {
            Orientation = StackOrientation.Horizontal;
            VerticalOptions = LayoutOptions.FillAndExpand;
            HorizontalOptions = LayoutOptions.FillAndExpand;
            Padding = new Thickness(0);
            Margin = new Thickness(0);
            Spacing = Metrics.ToolBarItemsSpacing;
        }
    }

    public class ContentPageExContent : StackLayout
    {
        public ContentPageExContent()
        {
            Orientation = StackOrientation.Vertical;
            VerticalOptions = LayoutOptions.FillAndExpand;
            HorizontalOptions = LayoutOptions.FillAndExpand;
            Padding = new Thickness(0);
            Margin = new Thickness(0);
            Spacing = 0;


        }
    }

    //[XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ContentPageEx : ContentPage
    {
        public ContentPageEx()
        {
            InitializeComponent();
        }

        //

        public Layout<View> AppBarRow
        {
            get { return _AppBarRow; }
        }

        public IList<View> AppBar
        {
            get { return _AppBarRow.Children; }
        }

        public Layout<View> ContentRow
        {
            get { return _ContentRow; }
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

        public static void LayoutAppBarAndToolBar(double width, double height, Grid Grid, Layout<View> AppBarRow, Layout<View> ToolBarRow)
        {
            bool IsLandscape = (DeviceEx.Orientation == DeviceOrientations.Landscape);

            bool StatusBarVisible = DeviceEx.StatusBarVisible;

            Grid.RowDefinitions[0].Height =
                (IsLandscape ? Metrics.AppBarHeightLandscape : Metrics.AppBarHeightPortrait)
                + ((StatusBarVisible) ? Metrics.StatusBarHeight : 0);

            AppBarRow.Padding = new Thickness(
                0,
                (StatusBarVisible ? Metrics.StatusBarHeight : 0),
                0,
                0);

            if (ToolBarRow.Children?.Count > 0)
                Grid.RowDefinitions[2].Height = (IsLandscape ? Metrics.ToolBarHeightLandscape : Metrics.ToolBarHeightPortrait);
        }

        Size _osa;

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (!Tools.IsDifferent(new Size(width, height), ref _osa))
                return;

            LayoutAppBarAndToolBar(width, height, _Grid, _AppBarRow, _ToolBarRow);
            OnLayoutFixed();
        }

        protected virtual void OnLayoutFixed()
        {
        }

    }
}
