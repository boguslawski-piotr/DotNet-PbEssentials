using System;
using Xamarin.Forms;

namespace pbXForms
{
#if __IOS__
	public class FlatButton : Button
#else
	public class FlatButton : ContentView
#endif
	{
#if !__IOS__
		private Label _Label
		{
			get
			{
				return (Xamarin.Forms.Label)(Content as StackLayout).Children[1];
			}
		}
	
		public string Text
		{
			set
			{
				_Label.Text = value;
				_Label.IsVisible = true;
			}
		}

		public Color TextColor
		{
			set
			{
				_Label.TextColor = value;
			}
		}

		public FontAttributes FontAttributes 
		{ 
			set
			{
				_Label.FontAttributes = value;
			}
		}

		public string FontFamily
		{
			set
			{
				_Label.FontFamily = value;
			}
		}

		public double FontSize
		{
			set
			{
				_Label.FontSize = value;
			}
		}

		private Image _Image
		{
			get 
			{
				return (Xamarin.Forms.Image)(Content as StackLayout).Children[0];
			}
		}

		public FileImageSource Image
		{
			set {
				_Image.Source = value;
				_Image.IsVisible = true;
			}
		}

		public event EventHandler Clicked;
#endif

		public FlatButton()
		{
			HeightRequest = Metrics.TouchTargetHeight;
			MinimumWidthRequest = Metrics.TouchTargetHeight;

			VerticalOptions = LayoutOptions.Center;

			Margin = new Thickness(0);

#if !__IOS__

			Content = new StackLayout()
			{
				Orientation = StackOrientation.Horizontal,
				VerticalOptions = LayoutOptions.CenterAndExpand,
				HorizontalOptions = LayoutOptions.CenterAndExpand,

				Margin = new Thickness(0),
				Padding = new Thickness(0),
				Spacing = Metrics.ButtonItemsSpacing,

				Children = {
					new Image() {
						IsVisible = false,
						HeightRequest = Metrics.IconHeight,
						WidthRequest = Metrics.IconHeight,
						Aspect = Aspect.AspectFit,
					},
					new Label() {
						IsVisible = false,
						VerticalTextAlignment = TextAlignment.Center,
					}
				}
			};
				
			TapGestureRecognizer tgr = new TapGestureRecognizer()
			{
				Command = new Command(OnTapped)
			};
			this.GestureRecognizers.Add(tgr);
#endif
		}

#if !__IOS__
		void OnTapped(object parameter)
		{
			Clicked?.Invoke(this, null);
		}
#endif
	}
}
