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
        protected ContentPageExView _Content => (ContentPageExView)Content;

        public ContentPageExAppBar AppBar => _Content.AppBar;
        public IList<View> AppBarContent => _Content.AppBarContent;

        public IList<View> PageContent => _Content.ViewContent;

        public ContentPageExToolBar ToolBar => _Content.ToolBar;
        public IList<View> ToolBarContent => _Content.ToolBarContent;

        public ContentPageEx()
        {
            InitializeComponent();
        }
    }
}
