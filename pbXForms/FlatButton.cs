using System;
using Xamarin.Forms;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using static Xamarin.Forms.Button;

namespace pbXForms
{
    public class FlatButton : ContentView
    {
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
		
        public event EventHandler Clicked;

		public static readonly BindableProperty ContentLayoutProperty =
            BindableProperty.Create("ContentLayout", typeof(ButtonContentLayout), typeof(FlatButton), new ButtonContentLayout(ButtonContentLayout.ImagePosition.Left, Metrics.ButtonItemsSpacing));

        public ButtonContentLayout ContentLayout
        {
            get { return (ButtonContentLayout)GetValue(ContentLayoutProperty); }
            set { SetValue(ContentLayoutProperty, value); }
        }

        // TODO: bindable?

        private Label _Label { get; set; }

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

        private Image _Image { get; set; }

        private FileImageSource _ImageSource = null;
        public FileImageSource Image
        {
            get { return _ImageSource; }
            set {
                _ImageSource = value;
                _Image.Source = value;
                _Image.IsVisible = true;
            }
        }

        public FlatButton()
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

        protected override void OnPropertyChanged(string propertyName = null)
        {
            if (propertyName == ContentLayoutProperty.PropertyName)
            {
                if (Content is StackLayout c)
                {
                    bool vert = ContentLayout.Position == ButtonContentLayout.ImagePosition.Bottom || ContentLayout.Position == ButtonContentLayout.ImagePosition.Top;
                    c.Orientation = vert ? StackOrientation.Vertical : StackOrientation.Horizontal;
                    c.Spacing = ContentLayout.Spacing;

                    if (ContentLayout.Position == ButtonContentLayout.ImagePosition.Top || ContentLayout.Position == ButtonContentLayout.ImagePosition.Right)
                        c.RaiseChild(_Image);
                }
            }

            base.OnPropertyChanged(propertyName);
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

        volatile Int32 _onTappedIsRunning = 0;

        void OnTapped(object parameter)
        {
            if (!IsEnabled || Interlocked.Exchange(ref _onTappedIsRunning, 1) == 1)
                return;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
#pragma warning restore CS4014
            {
                await this.ScaleTo(1.25, (uint)(DeviceEx.AnimationsLength * 0.65), Easing.CubicOut);
                await this.ScaleTo(1, (uint)(DeviceEx.AnimationsLength * 0.35), Easing.CubicIn);
            });

            Command?.Execute(CommandParameter);
            Clicked?.Invoke(this, EventArgs.Empty);

            Interlocked.Exchange(ref _onTappedIsRunning, 0);

            //Debug.WriteLine($"FlatButton: OnTapped: run for {DateTime.Now - s}");
        }
    }
}
