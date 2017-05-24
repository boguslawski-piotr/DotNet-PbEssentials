using System;
using Xamarin.Forms;

namespace pbXForms
{
    public class AppBarLayout : StackLayout
    {
        public AppBarLayout()
        {
            Orientation = StackOrientation.Horizontal;
            VerticalOptions = LayoutOptions.FillAndExpand;
            HorizontalOptions = LayoutOptions.FillAndExpand;
            Padding = new Thickness(0);
            Margin = new Thickness(0);
            //Spacing = Metrics.ToolBarItemsSpacing;
            Spacing = 0;
        }
    }

    public class ToolBarLayout : AppBarLayout
    { }

    public class ToolBarGridLayout : Grid
    {
        public ToolBarGridLayout()
        {
            Padding = new Thickness(0);
            Margin = new Thickness(0);
            ColumnSpacing = Metrics.ToolBarItemsWideSpacing;
            RowSpacing = 0;
            HorizontalOptions = LayoutOptions.CenterAndExpand;

            RowDefinitions = new RowDefinitionCollection() {
                new RowDefinition() {
                    Height = GridLength.Star
                },
            };
        }
    }

}
