using System;
using Xamarin.Forms;
using System.Windows.Input;
using System.Threading.Tasks;

namespace pbXForms
{
#if __IOS__
	public class FlatButton : Button
#else
	public class FlatButton : ContentView
#endif
	{
#if !__IOS__

		// TODO: public Button+ButtonContentLayout ContentLayout { get; set; }

		public static readonly BindableProperty CommandProperty = BindableProperty.Create("Command", typeof(ICommand), typeof(FlatButton), null,
			propertyChanged: (bo, o, n) => ((FlatButton)bo).OnCommandChanged());

		public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create("CommandParameter", typeof(object), typeof(FlatButton), null,
			propertyChanged: (bindable, oldvalue, newvalue) => ((FlatButton)bindable).CommandCanExecuteChanged(bindable, EventArgs.Empty));

		public ICommand Command
		{
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}

		public object CommandParameter
		{
			get { return GetValue(CommandParameterProperty); }
			set { SetValue(CommandParameterProperty, value); }
		}

		// TODO: bindable?

		private Label _Label
		{
			get { return (Xamarin.Forms.Label)(Content as StackLayout).Children[1]; }
		}

		public string Text
		{
			get { return _Label.Text; }
			set
			{
				_Label.Text = value;
				_Label.IsVisible = _Label.Text?.Length > 0;
			}
		}

		public Color TextColor
		{
			get { return _Label.TextColor; }
			set { _Label.TextColor = value; }
		}

		public FontAttributes FontAttributes
		{
			get { return _Label.FontAttributes; }
			set { _Label.FontAttributes = value; }
		}

		public string FontFamily
		{
			get { return _Label.FontFamily; }
			set { _Label.FontFamily = value; }
		}

		[TypeConverter(typeof(FontSizeConverter))]
		public double FontSize
		{
			get { return _Label.FontSize; }
			set { _Label.FontSize = value; }
		}


		private Image _Image
		{
			get { return (Xamarin.Forms.Image)(Content as StackLayout).Children[0]; }
		}

		private FileImageSource _ImageSource = null;
		public FileImageSource Image
		{
			get { return _ImageSource; }
			set
			{
				_ImageSource = value;
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

			//BackgroundColor = Color.Red;

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

			TapGestureRecognizer tgr = new TapGestureRecognizer();
			tgr.Command = new Command(OnTapped);
			this.GestureRecognizers.Add(tgr);
#endif
		}

#if !__IOS__
		protected override void OnPropertyChanging(string propertyName = null)
		{
			if (propertyName == CommandProperty.PropertyName)
			{
				ICommand cmd = Command;
				if (cmd != null)
					cmd.CanExecuteChanged -= CommandCanExecuteChanged;
			}

			base.OnPropertyChanging(propertyName);
		}

		void CommandCanExecuteChanged(object sender, EventArgs eventArgs)
		{
			ICommand cmd = Command;
			if (cmd != null)
			{
				//IsEnabledCore = cmd.CanExecute(CommandParameter);
				// TODO: enable/disable
			}
		}

		void OnCommandChanged()
		{
			if (Command != null)
			{
				Command.CanExecuteChanged += CommandCanExecuteChanged;
				CommandCanExecuteChanged(this, EventArgs.Empty);
			}
			else
			{
				//IsEnabledCore = true;
				// TODO: enable
			}
		}

		async void OnTapped(object parameter)
		{
			double opacity = Opacity;

			await this.FadeTo(0.2, 150);

			Command?.Execute(CommandParameter);
			Clicked?.Invoke(this, EventArgs.Empty);

			this.FadeTo(opacity, 150);
		}
#endif
	}
}
