using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace pbXForms
{
    public class ModalViewsManager
    {
        protected AbsoluteLayout _Layout;
        protected BoxView _Blocker;
        protected Frame _ModalView;

        public virtual bool HasShadow { get; set; } = false;
		public virtual float CornerRadius { get; set; } = 6;
		public virtual uint AnimationLength { get; set; } = 250;
		public virtual double BlockerOpacity { get; set; } = 0.50;
		public virtual Color BlokerBackgroundColor { get; set; } = Color.FromHex("#000000");

		public virtual void InitializeComponent(AbsoluteLayout layout)
        {
            _Layout = layout;

            if (_Layout.Children.Count < 1)
                throw new Exception("ModalViewsManager: Layout (AbsoluteLayout) must contain at least one view == main view.");

            View view = _Layout.Children[0];
            AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.SizeProportional);
            AbsoluteLayout.SetLayoutBounds(view, new Rectangle(0, 0, 1, 1));

            _Blocker = new BoxView()
            {
                BackgroundColor = Color.Transparent,
                IsVisible = false,
            };
            _Blocker.GestureRecognizers.Add(
                new TapGestureRecognizer()
                {
                    Command = new Command(async (object parameter) =>
                    {
                        await PopModalAsync();
                    }),
                });
            AbsoluteLayout.SetLayoutFlags(_Blocker, AbsoluteLayoutFlags.SizeProportional);
            AbsoluteLayout.SetLayoutBounds(_Blocker, new Rectangle(0, 0, 1, 1));
            _Layout.Children.Add(_Blocker);

            _ModalView = new Frame()
            {
                IsVisible = false,
                HasShadow = HasShadow,
                OutlineColor = Color.Transparent,
                CornerRadius = CornerRadius,
                Margin = new Thickness(0),
                Padding = new Thickness(2),
                HorizontalOptions = LayoutOptions.StartAndExpand,
                VerticalOptions = LayoutOptions.StartAndExpand
            };
            AbsoluteLayout.SetLayoutFlags(_ModalView, AbsoluteLayoutFlags.None);
            _Layout.Children.Add(_ModalView);
        }

        public virtual void OnSizeAllocated(double width, double height)
        {
            if (_ModalView.IsVisible)
                AnimateModalAsync(false, false);
        }

        public enum ModalPosition
        {
            Center,
            BottomCenter,
            BottomLeft,
            BottomRight,
            TakeWholeView,
        }

        ModalPosition modalPosition = ModalPosition.Center;

        public virtual async Task PushModalAsync(ContentView content, ModalPosition position = ModalPosition.Center, bool animate = true)
        {
            // Initialize...

            _ModalView.BackgroundColor = content.BackgroundColor;
            _ModalView.Content = content;

            // Show...

            modalPosition = position;
            await AnimateModalAsync(false, animate);
        }

        public virtual async Task PopModalAsync(bool animate = true)
        {
            if (!_ModalView.IsVisible)
                return;

            await AnimateModalAsync(true, animate);
            CancellationToken?.Cancel();
        }

		protected virtual async Task AnimateModalAsync(bool hide, bool animate)
		{
            // Calculate size and position...

            _ModalView.HasShadow = HasShadow;
            _ModalView.CornerRadius = CornerRadius;

			Rectangle bounds = _Layout.Bounds.Inflate(-(Metrics.ScreenEdgeMargin), -(Metrics.ScreenEdgeMargin));
			Xamarin.Forms.Layout.LayoutChildIntoBoundingRegion(_ModalView, bounds);

			Rectangle to = _ModalView.Bounds;
			if (modalPosition == ModalPosition.Center)
			{
				to.X = _Layout.Bounds.Width / 2 - to.Width / 2;
				to.Y = _Layout.Bounds.Height / 2 - to.Height / 2;
			}
			else if (modalPosition == ModalPosition.BottomCenter)
			{
				to.X = _Layout.Bounds.Width / 2 - to.Width / 2;
				to.Y = _Layout.Bounds.Bottom - to.Height - Metrics.ScreenEdgeMargin;
			}
			else if (modalPosition == ModalPosition.BottomLeft)
			{
				to.X = Metrics.ScreenEdgeMargin;
				to.Y = _Layout.Bounds.Bottom - to.Height - Metrics.ScreenEdgeMargin;
			}
			else if (modalPosition == ModalPosition.BottomRight)
			{
                to.X = _Layout.Bounds.Width - to.Width - Metrics.ScreenEdgeMargin;
				to.Y = _Layout.Bounds.Bottom - to.Height - Metrics.ScreenEdgeMargin;
			}

			Rectangle from = to;
            if(animate)
			    from.Y = _Layout.Bounds.Height;

			AbsoluteLayout.SetLayoutBounds(_ModalView, hide ? to : from);
			_ModalView.Layout(hide ? to : from);

			// Show/hide with optional animation...

            _Blocker.Opacity = hide || !animate ? BlockerOpacity : 0;
            _Blocker.BackgroundColor = BlokerBackgroundColor;

            _Blocker.IsVisible = true;
            _ModalView.IsVisible = true;

            if (animate)
            {
                await Task.WhenAny(
                    _ModalView.LayoutTo(hide ? from : to, AnimationLength),
                    _Blocker.FadeTo(hide ? 0 : BlockerOpacity, AnimationLength)
                );
			}

			_Blocker.IsVisible = !hide;
			_ModalView.IsVisible = !hide;
		}


        //

        CancellationTokenSource CancellationToken;

        public virtual async Task<bool> DisplayModalAsync(ModalContentView content, ModalPosition position = ModalPosition.Center, bool animate = true)
		{
            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            CancellationToken = cancellationToken;
			
            bool rc = false;

            content.OK += (sender, e) => { rc = true; cancellationToken.Cancel(); };
			content.Cancel += (sender, e) => cancellationToken.Cancel();

            await PushModalAsync(content, position, animate);
            try
            {
                // This is not the nicest solution, but good enough ;)
				await Task.Delay(TimeSpan.FromHours(24), cancellationToken.Token);
            }
            catch (TaskCanceledException ex)
            { 
            }

            await PopModalAsync();
            CancellationToken = null;

            return rc;
		}
	}
}
