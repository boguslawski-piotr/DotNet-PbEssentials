using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace AbsoluteLayoutUWPBug
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();

			AbsoluteLayout layout = new AbsoluteLayout();

			AbsoluteLayout viewsLayout = new AbsoluteLayout();
			AbsoluteLayout.SetLayoutFlags(viewsLayout, AbsoluteLayoutFlags.SizeProportional);
			AbsoluteLayout.SetLayoutBounds(viewsLayout, new Rectangle(0, 0, 1, 1));

			ContentView view1 = new ContentView()
			{
				BackgroundColor = Color.LightBlue,
				Content = new StackLayout
				{
					Orientation = StackOrientation.Vertical,
					VerticalOptions = LayoutOptions.CenterAndExpand,
					HorizontalOptions = LayoutOptions.CenterAndExpand,
				}
			};
			ContentView view2 = new ContentView()
			{
				BackgroundColor = Color.LightGreen,
				Content = new StackLayout
				{
					Orientation = StackOrientation.Vertical,
					VerticalOptions = LayoutOptions.CenterAndExpand,
					HorizontalOptions = LayoutOptions.CenterAndExpand,
				}
			};

			(view1.Content as StackLayout).Children.Add(new Button
			{
				Text = "show view 2",
				Command = new Command(() =>
				{
					viewsLayout.RaiseChild(view2);
					viewsLayout.ForceLayout();
				})
			});
			AbsoluteLayout.SetLayoutFlags(view1, AbsoluteLayoutFlags.SizeProportional);
			AbsoluteLayout.SetLayoutBounds(view1, new Rectangle(0, 0, 1, 1));

			viewsLayout.Children.Add(view1);

			(view2.Content as StackLayout).Children.Add(new Button
			{
				Text = "show view 1",
				Command = new Command(() =>
				{
					viewsLayout.RaiseChild(view1);
				})
			});
			AbsoluteLayout.SetLayoutFlags(view2, AbsoluteLayoutFlags.SizeProportional);
			AbsoluteLayout.SetLayoutBounds(view2, new Rectangle(0, 0, 1, 1));

			viewsLayout.Children.Add(view2);

			layout.Children.Add(viewsLayout);

			viewsLayout.RaiseChild(view1);

			Content = layout;
		}
	}
}
