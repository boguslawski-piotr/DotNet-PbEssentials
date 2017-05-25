using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace pbXForms
{
    public interface INavigationEx
    {
    }

    public class NavigationEx : INavigationEx
    {
        protected VisualElement _View;  // can be ContentPage or ContentView :)
        protected BoxView _Blocker;
        protected Frame _Dialog;

        public virtual void InitializeComponent(VisualElement view, Layout<View> viewContent, BoxView blocker, Frame dialog)
		{
            // TODO: niech zwraca AbsoluteLayout, i sam tworzy blocker i dialog -> ten kto wywoluje musi tylko podmienic Content = zwrocony AL

            _View = view;
            _Blocker = blocker;
            _Dialog = dialog;

			AbsoluteLayout.SetLayoutFlags(viewContent, AbsoluteLayoutFlags.SizeProportional);
			AbsoluteLayout.SetLayoutBounds(viewContent, new Rectangle(0, 0, 1, 1));

			AbsoluteLayout.SetLayoutFlags(_Blocker, AbsoluteLayoutFlags.SizeProportional);
			AbsoluteLayout.SetLayoutBounds(_Blocker, new Rectangle(0, 0, 1, 1));
			blocker.GestureRecognizers.Add(
				new TapGestureRecognizer()
				{
                    Command = new Command(async (object parameter) => 
					{
						await PopModalAsync();
					}),
				});
		}

        public virtual void OnSizeAllocated(double width, double height)
        {
			if (_Dialog.IsVisible)
				AnimateModalAsync(false, false);
		}

		public enum ModalPosition
		{
			Center,
			BottomCenter,
            BottomLeft,
            BottomRight,
		}

		protected ModalPosition modalPosition = ModalPosition.Center;

		public virtual async Task PushModalAsync(ContentView content, ModalPosition position = ModalPosition.Center, bool animate = true)
		{
			// Initialize...

			_Dialog.HorizontalOptions = LayoutOptions.StartAndExpand;
			_Dialog.VerticalOptions = LayoutOptions.StartAndExpand;
			_Dialog.BackgroundColor = content.BackgroundColor;
			_Dialog.Content = content;

			// Show...

			modalPosition = position;
			await AnimateModalAsync(false, animate);
		}

		public virtual async Task PopModalAsync(bool animate = true)
		{
			if (!_Dialog.IsVisible)
				return;

			await AnimateModalAsync(true, animate);
		}

		protected virtual async Task AnimateModalAsync(bool hide, bool animate)
		{
			// Calculate size and position...

			Rectangle bounds = _View.Bounds.Inflate(-(Metrics.ScreenEdgeMargin), -(Metrics.ScreenEdgeMargin));
			Xamarin.Forms.Layout.LayoutChildIntoBoundingRegion(_Dialog, bounds);

			AbsoluteLayout.SetLayoutFlags(_Dialog, AbsoluteLayoutFlags.None);

			Rectangle to = _Dialog.Bounds;
			if (modalPosition == ModalPosition.Center)
			{
				to.X = _View.Bounds.Width / 2 - to.Width / 2;
				to.Y = _View.Bounds.Height / 2 - to.Height / 2;
			}
			else if (modalPosition == ModalPosition.BottomCenter)
			{
				to.X = _View.Bounds.Width / 2 - to.Width / 2;
				to.Y = _View.Bounds.Bottom - to.Height - Metrics.ScreenEdgeMargin;
			}
			else if (modalPosition == ModalPosition.BottomLeft)
			{
				to.X = Metrics.ScreenEdgeMargin;
				to.Y = _View.Bounds.Bottom - to.Height - Metrics.ScreenEdgeMargin;
			}
			else if (modalPosition == ModalPosition.BottomRight)
			{
                to.X = _View.Bounds.Width - to.Width - Metrics.ScreenEdgeMargin;
				to.Y = _View.Bounds.Bottom - to.Height - Metrics.ScreenEdgeMargin;
			}

			Rectangle from = to;
			from.Y = _View.Bounds.Height;

			AbsoluteLayout.SetLayoutBounds(_Dialog, hide ? to : from);
			_Dialog.Layout(hide ? to : from);

			// Show/hide with optional animation...

			const uint length = 250;
			//const double dopacity = 1;
			const double bopacity = 0.8;

			//_Dialog.Opacity = hide ? dopacity : 0;
			_Blocker.Opacity = hide ? bopacity : 0;
			_Blocker.BackgroundColor = Color.FromHex("#afafaf");

			_Blocker.IsVisible = true;
			_Dialog.IsVisible = true;

			await Task.WhenAny(
				_Dialog.LayoutTo(hide ? from : to, length),
				//_Dialog.FadeTo(hide ? 0 : dopacity, length),
				_Blocker.FadeTo(hide ? 0 : bopacity, length)
			);

			_Blocker.IsVisible = !hide;
			_Dialog.IsVisible = !hide;
		}
	}
}
