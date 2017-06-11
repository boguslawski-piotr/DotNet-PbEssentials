using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using static Xamarin.Forms.Button;

namespace pbXForms
{
	public class PIButton : ContentView
	{
		public static readonly BindableProperty CommandProperty = BindableProperty.Create("Command", typeof(ICommand), typeof(PIButton), null,
			propertyChanged: (bo, o, n) => ((PIButton)bo).OnCommandChanged());

		public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create("CommandParameter", typeof(object), typeof(PIButton), null,
			propertyChanged: (bo, o, n) => ((PIButton)bo).CommandCanExecuteChanged(bo, EventArgs.Empty));

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

		public event EventHandler Clicked;

		public static readonly BindableProperty ContentLayoutProperty = BindableProperty.Create("ContentLayout", typeof(ButtonContentLayout), typeof(PIButton),
			new ButtonContentLayout(ButtonContentLayout.ImagePosition.Left, Metrics.ButtonItemsSpacing),
			propertyChanged: (bo, o, n) => ((PIButton)bo).OnContentLayoutChanged());

		public ButtonContentLayout ContentLayout
		{
			get { return (ButtonContentLayout)GetValue(ContentLayoutProperty); }
			set { SetValue(ContentLayoutProperty, value); }
		}

		// TODO: bindable?

		Label _Label { get; set; }

		public string Text
		{
			get { return _Label.Text; }
			set {
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

		Image _Image { get; set; }

		FileImageSource _ImageSource;
		public FileImageSource Image
		{
			get { return _ImageSource; }
			set {
				_ImageSource = value;
				_Image.Source = value;
				_Image.IsVisible = true;
			}
		}

		public PIButton()
		{
			HeightRequest = Metrics.TouchTargetHeight;
			MinimumHeightRequest = Metrics.TouchTargetHeight;
			WidthRequest = Metrics.TouchTargetHeight;
			MinimumWidthRequest = Metrics.TouchTargetHeight;

			VerticalOptions = LayoutOptions.Center;
			Margin = new Thickness(0);

			_Image = new Image()
			{
				IsVisible = false,
				HeightRequest = Metrics.IconHeight,
				WidthRequest = Metrics.IconHeight,
				Aspect = Aspect.AspectFit,
			};

			_Label = new Label()
			{
				IsVisible = false,
				VerticalTextAlignment = TextAlignment.Center,
				HorizontalTextAlignment = TextAlignment.Center,
			};

			Content = new StackLayout()
			{
				Orientation = StackOrientation.Horizontal,
				VerticalOptions = LayoutOptions.CenterAndExpand,
				HorizontalOptions = LayoutOptions.CenterAndExpand,

				Margin = new Thickness(0),
				Padding = new Thickness(0),
				Spacing = ContentLayout.Spacing,

				Children = {
					_Image,
					_Label
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

		bool _IsEnabled
		{
			set {
				IsEnabled = value;
				Content.IsEnabled = IsEnabled;
				_Image.IsEnabled = IsEnabled;
				_Label.IsEnabled = IsEnabled;
			}
		}

		void CommandCanExecuteChanged(object sender, EventArgs eventArgs)
		{
			ICommand cmd = Command;
			if (cmd != null)
			{
				_IsEnabled = cmd.CanExecute(CommandParameter);
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
				_IsEnabled = true;
		}

		void OnContentLayoutChanged()
		{
			if (Content is StackLayout c)
			{
				bool vert = ContentLayout.Position == ButtonContentLayout.ImagePosition.Bottom || ContentLayout.Position == ButtonContentLayout.ImagePosition.Top;

				c.Orientation = vert ? StackOrientation.Vertical : StackOrientation.Horizontal;
				c.Spacing = ContentLayout.Spacing;

				if (ContentLayout.Position == ButtonContentLayout.ImagePosition.Top || ContentLayout.Position == ButtonContentLayout.ImagePosition.Right)
				{
					if (c.Children[0] == _Image)
						c.RaiseChild(_Image);
				}
			}
		}

		volatile Int32 _onTappedIsRunning = 0;

		void OnTapped(object parameter)
		{
			if (!IsEnabled || Interlocked.Exchange(ref _onTappedIsRunning, 1) == 1)
				return;

			StartTappedAnimation();

			Command?.Execute(CommandParameter);
			Clicked?.Invoke(this, EventArgs.Empty);

			Interlocked.Exchange(ref _onTappedIsRunning, 0);
		}

		protected virtual void StartTappedAnimation()
		{
			Task.Run(async () =>
			{
				await this.ScaleTo(1.25, (uint)(DeviceEx.AnimationsLength * 0.65), Easing.CubicOut);
				await this.ScaleTo(1, (uint)(DeviceEx.AnimationsLength * 0.35), Easing.CubicIn);
			});

		}
	}
}
