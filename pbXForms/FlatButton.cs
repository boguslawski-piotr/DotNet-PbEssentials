using System;
using Xamarin.Forms;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Diagnostics;

namespace pbXForms
{
    public class FlatButton : ContentView
    {

		// TODO: public Button+ButtonContentLayout ContentLayout { get; set; }

		public static readonly BindableProperty CommandProperty = BindableProperty.Create("Command", typeof(ICommand), typeof(FlatButton), null,
			propertyChanged: (bo, o, n) => ((FlatButton)bo).OnCommandChanged());

		public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create("CommandParameter", typeof(object), typeof(FlatButton), null,
			propertyChanged: (bo, o, n) => ((FlatButton)bo).CommandCanExecuteChanged(bo, EventArgs.Empty));

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

		public LineBreakMode LineBreakMode
		{
            get { return _Label.LineBreakMode; }
			set { _Label.LineBreakMode = value; }
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

        public FlatButton()
        {
            HeightRequest = Metrics.TouchTargetHeight;

            MinimumWidthRequest = Metrics.TouchTargetHeight;

            VerticalOptions = LayoutOptions.Center;

            Margin = new Thickness(0);

            WidthRequest = Metrics.TouchTargetHeight;

			//BackgroundColor = Color.Red;

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
                        TextColor = (Color)Application.Current.Resources["AccentTextColor"], // TODO: do przemyslenia!
						VerticalTextAlignment = TextAlignment.Center,
					}
				}
			};

			TapGestureRecognizer tgr = new TapGestureRecognizer();
			tgr.Command = new Command(OnTapped);
			this.GestureRecognizers.Add(tgr);
        }

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
				IsEnabled = cmd.CanExecute(CommandParameter);
                // TODO: visual changes?
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
				IsEnabled = true;
				// TODO: visual changes?
			}
		}

        volatile bool _onTappedIsRunning = false;

		async void OnTapped(object parameter)
		{
            if(!IsEnabled || _onTappedIsRunning)
                return;
			
            DateTime s = DateTime.Now;

			_onTappedIsRunning = true;

            double opacity = Opacity;

			await this.ScaleTo(1.33, 150, Easing.CubicOut);

			Command?.Execute(CommandParameter);
            Clicked?.Invoke(this, EventArgs.Empty);
			
            this.ScaleTo(1, 150, Easing.CubicIn);

            _onTappedIsRunning = false;

			//Debug.WriteLine($"FlatButton: OnTapped: run for {DateTime.Now - s}");
		}
    }
}
