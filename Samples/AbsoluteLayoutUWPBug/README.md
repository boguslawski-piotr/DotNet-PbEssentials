### Steps to reproduce

Please replace ```public MainPage()``` in newly created XF project with code below.

```
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
```

### Expected behavior

It should (and it does on iOS, Android and macOS) show view2 when clicked on button in view1 and show view1 when clicked on button in view2.

### Actual behavior

On UWP it does nothing at all :(
On iOS, Android and macOS it works OK, as I expected.

### Supplemental info (logs, images, videos)

Full example can be found here:
https://github.com/boguslawski-piotr/pbX/tree/master/Samples

More complicated and useful code using this technique (works ok on iOS, Android and macOS but not on UWP) can be found here:
https://github.com/boguslawski-piotr/pbX/blob/master/pbXForms/MastersDetailsPage.xaml.cs

### Test environment (full version information)

Visual Studio 2017 on Windows
Visual Studio for Mac alpha
Xamarin.Forms 2.3.5-239-pre3

			  