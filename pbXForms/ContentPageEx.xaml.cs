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

    public class ContentPageExAppBar : StackLayout
    {
        public ContentPageExAppBar()
        {
            Orientation = StackOrientation.Horizontal;
            VerticalOptions = LayoutOptions.FillAndExpand;
            HorizontalOptions = LayoutOptions.FillAndExpand;
            Padding = new Thickness(0);
            Margin = new Thickness(0);
            Spacing = Metrics.ToolBarItemsSpacing;
        }
    }

    public class ContentPageExToolBar : ContentPageExAppBar
    { }

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

        ContentPageExView _Content => (ContentPageExView)Content;
        Grid _View => _Content._View;

        //public IList<View> AppBarContent => _AppBarRow.Children;
        Layout<View> _AppBarRow => _Content.AppBarRow;
        public IList<View> AppBarContent => _Content.AppBarContent;

        //public IList<View> ContentEx => _ContentRow.Children;
        public IList<View> ContentEx => _Content.ContentEx;

        //public IList<View> ToolBarContent => _ToolBarRow.Children;
        Layout<View> _ToolBarRow => _Content.ToolBarRow;
        public IList<View> ToolBarContent => _Content.ToolBarContent;

        //

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

            if (!Tools.IsDifferent(new Size(width, height), ref _osa))
                return;

            LayoutAppBarAndToolBar(width, height, _View, _AppBarRow, _ToolBarRow);

            OnLayoutFixed();
        }

        protected virtual void OnLayoutFixed()
        {
        }

    }
}
