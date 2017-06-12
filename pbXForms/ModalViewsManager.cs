using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
	public class ModalViewsManager
	{
		/// Modal position.
		public enum ModalPosition
		{
			Center,
			BottomCenter,
			BottomLeft,
			BottomRight,
			TopCenter,
			TopLeft,
			TopRight,
			LeftCenter,
			RightCenter,
			NavDrawer,
			WholeView,
		}

		/// Gets or sets the background color of the bloker.
		public Color BlokerBackgroundColor { get; set; } = Color.FromHex("#000000");

		/// Gets or sets the blocker opacity applied when modal is displayed.
		public double BlockerOpacity { get; set; } = 0.50;

		public event EventHandler<ModalContentView> ModalWillBeShown;

		public event EventHandler<ModalContentView> ModalHasBeenShown;

		public event EventHandler<ModalContentView> ModalWillBeHidden;

		public event EventHandler<ModalContentView> ModalHasBeenHidden;

		//

		protected AbsoluteLayout Layout;

		protected class Modal
		{
			public ModalPosition position;
			public BoxView blocker;
			public Xamarin.Forms.ContentView view;
		};

		protected Stack<Modal> ModalsStack = new Stack<Modal>();
		protected Stack<CancellationTokenSource> CancellationTokensStack = new Stack<CancellationTokenSource>();

		//

		public virtual void InitializeComponent(AbsoluteLayout layout)
		{
			Layout = layout;

			if (Layout == null || Layout.Children.Count < 1)
				throw new Exception("ModalViewsManager: Layout (AbsoluteLayout) must contain at least one view == main view.");

			View view = Layout.Children[0];
			AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.SizeProportional);
			AbsoluteLayout.SetLayoutBounds(view, new Rectangle(0, 0, 1, 1));
		}

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

		public virtual void OnSizeAllocated(double width, double height)
		{
			foreach (var m in ModalsStack)
			{
				AnimateModalAsync(m, false, false);
			}
		}

#pragma warning restore CS4014

		public virtual async Task<bool> DisplayModalAsync(ModalContentView content, ModalPosition position = ModalPosition.Center, bool animate = true)
		{
			CancellationTokenSource cancellationToken = new CancellationTokenSource();
			CancellationTokensStack.Push(cancellationToken);

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
				Log.D(ex.Message, this);
			}

			await PopModalAsync();
			CancellationTokensStack.Pop();  // Do NOT move this line before PopModalAsync

			return rc;
		}

		public virtual async Task PushModalAsync(ModalContentView content, ModalPosition position = ModalPosition.Center, bool animate = true)
		{
			content.Position = position;

			Modal modal = new Modal()
			{
				position = position,
			};

			modal.blocker = new BoxView()
			{
				IsVisible = false,
				BackgroundColor = BlokerBackgroundColor,
			};
			modal.blocker.GestureRecognizers.Add(
				new TapGestureRecognizer()
				{
					Command = new Command(async (object parameter) =>
					{
						if (CancellationTokensStack.Count > 0)
							CancellationTokensStack.Peek().Cancel();
						else
							await PopModalAsync();
					}),
				});
			AbsoluteLayout.SetLayoutFlags(modal.blocker, AbsoluteLayoutFlags.SizeProportional);
			AbsoluteLayout.SetLayoutBounds(modal.blocker, new Rectangle(0, 0, 1, 1));
			Layout.Children.Add(modal.blocker);

			float cornerRadius = position >= ModalPosition.NavDrawer ? 0 : content.CornerRadius;
			modal.view = new Frame
			{
				IsVisible = false,
				BackgroundColor = content.BackgroundColor,
				HasShadow = content.HasShadow,
				OutlineColor = Color.Transparent,
				CornerRadius = cornerRadius,
				Margin = new Thickness(0),
				Padding = new Thickness(cornerRadius / 2),
				HorizontalOptions = LayoutOptions.StartAndExpand,
				VerticalOptions = LayoutOptions.StartAndExpand,
				Content = content,
			};
			AbsoluteLayout.SetLayoutFlags(modal.view, AbsoluteLayoutFlags.None);
			Layout.Children.Add(modal.view);

			ModalsStack.Push(modal);

			await AnimateModalAsync(modal, false, animate);
		}

		public virtual async Task PopModalAsync(bool animate = true)
		{
			if (ModalsStack.Count <= 0)
				return;

			Modal modal = ModalsStack.Pop();
			await AnimateModalAsync(modal, true, animate);

			Layout.Children.Remove(modal.view);
			Layout.Children.Remove(modal.blocker);

			if (CancellationTokensStack.Count > 0)
				CancellationTokensStack.Peek().Cancel();
		}

		//

		protected virtual async Task AnimateModalAsync(Modal modal, bool hide, bool animate)
		{
			// Calculate size and position...

			ModalContentView mvc = modal.view.Content as ModalContentView;

			double navDrawerWidth = DeviceEx.Orientation == DeviceOrientation.Landscape ? mvc.WidthInLandscapeWhenNavDrawer : mvc.WidthInPortraitWhenNavDrawer;
			navDrawerWidth = navDrawerWidth <= 0 ? Layout.Bounds.Width * mvc.RelativeWidthWhenNavDrawer : navDrawerWidth;

			Rectangle bounds;
			if (modal.position >= ModalPosition.NavDrawer)
			{
				bounds = Layout.Bounds;
				if (modal.position == ModalPosition.NavDrawer)
					bounds.Width = navDrawerWidth;
			}
			else
				bounds = Layout.Bounds.Inflate(-(Metrics.ScreenEdgeMargin), -(Metrics.ScreenEdgeMargin));

			Xamarin.Forms.Layout.LayoutChildIntoBoundingRegion(modal.view, bounds);

			Rectangle to = modal.view.Bounds;

			if (modal.position < ModalPosition.NavDrawer)
			{
				if (mvc.MinimumHeightRequest > 0)
					to.Height = Math.Min(bounds.Height, Math.Max(mvc.MinimumHeightRequest, to.Height));
				if (mvc.MaximumHeightRequest > 0)
					to.Height = Math.Min(mvc.MaximumHeightRequest, to.Height);

				if (mvc.MinimumWidthRequest > 0)
					to.Width = Math.Min(bounds.Width, Math.Max(mvc.MinimumWidthRequest, to.Width));
				if (mvc.MaximumWidthRequest > 0)
					to.Width = Math.Min(mvc.MaximumWidthRequest, to.Width);
			}

			switch (modal.position)
			{
				case ModalPosition.Center:
					to.X = Layout.Bounds.Width / 2 - to.Width / 2;
					to.Y = Layout.Bounds.Height / 2 - to.Height / 2;
					break;
				case ModalPosition.BottomCenter:
					to.X = Layout.Bounds.Width / 2 - to.Width / 2;
					to.Y = Layout.Bounds.Bottom - to.Height - Metrics.ScreenEdgeMargin;
					break;
				case ModalPosition.BottomLeft:
					to.X = Metrics.ScreenEdgeMargin;
					to.Y = Layout.Bounds.Bottom - to.Height - Metrics.ScreenEdgeMargin;
					break;
				case ModalPosition.BottomRight:
					to.X = Layout.Bounds.Width - to.Width - Metrics.ScreenEdgeMargin;
					to.Y = Layout.Bounds.Bottom - to.Height - Metrics.ScreenEdgeMargin;
					break;
				case ModalPosition.TopCenter:
					to.X = Layout.Bounds.Width / 2 - to.Width / 2;
					to.Y = Metrics.ScreenEdgeMargin;
					break;
				case ModalPosition.TopLeft:
					to.X = Metrics.ScreenEdgeMargin;
					to.Y = Metrics.ScreenEdgeMargin;
					break;
				case ModalPosition.TopRight:
					to.X = Layout.Bounds.Width - to.Width - Metrics.ScreenEdgeMargin;
					to.Y = Metrics.ScreenEdgeMargin;
					break;
				case ModalPosition.LeftCenter:
					to.X = Metrics.ScreenEdgeMargin;
					to.Y = Layout.Bounds.Height / 2 - to.Height / 2;
					break;
				case ModalPosition.RightCenter:
					to.X = Layout.Bounds.Width - to.Width - Metrics.ScreenEdgeMargin;
					to.Y = Layout.Bounds.Height / 2 - to.Height / 2;
					break;
				case ModalPosition.NavDrawer:
					to = new Rectangle(0, 0, navDrawerWidth, Layout.Bounds.Height);
					break;
				case ModalPosition.WholeView:
					to = Layout.Bounds;
					break;
			}

			Rectangle from = to;
			if (animate)
			{
				if (modal.position == ModalPosition.NavDrawer || modal.position == ModalPosition.LeftCenter)
					from.X -= to.Width;                     // from/to left
				else
				{
					if (modal.position == ModalPosition.RightCenter)
						from.X += Layout.Bounds.Width;     // from/to right
					else
					{
						if (modal.position == ModalPosition.TopCenter || modal.position == ModalPosition.TopLeft || modal.position == ModalPosition.TopRight)
							from.Y -= to.Height;            // from/to top
						else
							from.Y = Layout.Bounds.Height; // from/to bottom
					}
				}
			}

			// Layout to animation start / final position

			modal.view.WidthRequest = to.Width;
			modal.view.HeightRequest = to.Height;

			AbsoluteLayout.SetLayoutBounds(modal.view, hide ? to : from);
			modal.view.Layout(hide ? to : from);

			// Show/hide with optional animation...

			modal.blocker.Opacity = hide || !animate ? BlockerOpacity : 0;

			if (!hide)
			{
				mvc.OnAppearingWhenModal();
				ModalWillBeShown?.Invoke(this, mvc);
			}
			else
			{
				mvc.OnDisappearingWhenModal();
				ModalWillBeHidden?.Invoke(this, mvc);
			}
			
			modal.blocker.IsVisible = true;
			modal.view.IsVisible = true;

			if (animate)
			{
				uint al = DeviceEx.AnimationsLength;
				await Task.WhenAll(
					modal.view.LayoutTo(hide ? from : to, al, Easing.CubicOut),
					modal.blocker.FadeTo(hide ? 0 : BlockerOpacity, al / 2)
				);

				// This is really needed because LayoutTo is not permananet in this situation.
				AbsoluteLayout.SetLayoutBounds(modal.view, hide ? from : to);
				modal.view.Layout(hide ? from : to);
			}

			modal.blocker.IsVisible = !hide;
			modal.view.IsVisible = !hide;

			if (!hide)
				ModalHasBeenShown?.Invoke(this, mvc);
			else
				ModalHasBeenHidden?.Invoke(this, mvc);
		}
	}
}
