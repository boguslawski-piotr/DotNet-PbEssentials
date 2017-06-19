using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
	// All code that is included in the #if WINDOWS_UWP conditional build 
	// is a workaround for a bug in AbsoluteLayout.RaiseChild/LowerChild on the UWP platform.
	// On this platform, these features behave completely different than on iOS, macOS or Android.
	// To be completely honest - they do not work at all.

	public class MastersDetailsPageViewsLayout : AbsoluteLayout { }

	/// <summary>
	/// 
	/// </summary>
	/// <example>
	/// <para>A complete example showing MastersDetailsPage in action can be found here:</para>
	/// <para>https://github.com/boguslawski-piotr/pbX/tree/master/Samples/MDP/MDP</para>
	/// </example>
	public partial class MastersDetailsPage : Xamarin.Forms.ContentPage
	{
		/// <summary>
		/// Relative (to screen/application main window) width of master view(s) in a split view. Available also in XAML. Default: 0.3
		/// </summary>
		public double MasterViewRelativeWidth { get; set; } = 0.3;

		/// <summary>
		/// Minimum width of master view(s) in a split view. Available also in XAML. Default: 240 for phones and tablets and 320 for desktops.
		/// </summary>
		public double MasterViewMinimumWidth { get; set; } = Device.Idiom != TargetIdiom.Desktop ? 240 : 320;

		/// <summary>
		/// Allows you to set whether split view is available on phones. Available also in XAML. Default: true
		/// </summary>
		public bool AllowSplitViewOnPhone { get; set; } = true;

		/// <summary>
		/// List of master views. Available both in code and XAML files.
		/// </summary>
		public IList<View> MasterViews { get; set; } = new List<View>();

		/// <summary>
		/// List of detail views. Available both in code and XAML files.
		/// </summary>
		public IList<View> DetailViews => _ViewsLayout?.Children;

		/// <summary>
		/// Gives the width of the panel with master view(s) in a split view.
		/// </summary>
		public double MasterViewWidthInSplitView { get; protected set; }

		/// <summary>
		/// Manager for modal views that can be displayed on masters/details page.
		/// </summary>
		/// <seealso cref="ModalViewsManager"/>
		public ModalViewsManager ModalManager = new ModalViewsManager();

		protected View MasterView;
		protected View DetailView;

		protected IList<View> Views => _ViewsLayout?.Children;

		public MastersDetailsPage()
		{
			InitializeComponent();
			ModalManager.InitializeComponent(_Layout);
		}

		/// <summary>
		/// Initializes all master and detail views.
		/// Must be called AFTER InitializeComponent() of a class that inherits from this.
		/// </summary>
		public virtual void InitializeViews(bool showMasterView = true)
		{
			if (MasterViews.Count > 0)
			{
				MasterView = MasterViews[0];
				Views.Insert(0, MasterView);
			}
			else
			{
				if (Views == null || Views.Count <= 0)
					return;
				MasterView = Views[0];
			}

			DetailView = Views?.Count > 1 ? Views[1] : null;
			_MasterViewIsVisible = !IsSplitView;

			if (DetailView != null)
				_ViewsLayout.RaiseChild(DetailView);
			if (showMasterView)
				_ViewsLayout.RaiseChild(MasterView);
		}

		int _IsSplitView = -1;
		public virtual bool IsSplitView
		{
			get {
				if (_IsSplitView > -1)
					return _IsSplitView == 1;

				if (!AllowSplitViewOnPhone && Device.Idiom == TargetIdiom.Phone)
					return false;

				return DeviceEx.Orientation != DeviceOrientation.Portrait;
			}
			set {
				_IsSplitView = value ? 1 : 0;
				_osa = new Size(-1, -1);
				ForceLayout();
			}
		}

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

		bool _MasterViewIsVisible = true;
		public virtual bool MasterViewIsVisible
		{
			get => _MasterViewIsVisible;
			set {
				if (value)
					ShowMasterViewAsync(ViewsSwitchingAnimation.Back);
				else
					HideMasterViewAsync(ViewsSwitchingAnimation.Forward);
			}
		}

#pragma warning restore CS4014

		//

		public enum ViewsSwitchingAnimation
		{
			NoAnimation,
			Forward,
			Back,
		};

		public event EventHandler<(View view, object param)> MasterViewWillBeShown;
		public event EventHandler<(View view, object param)> MasterViewHasBeenShown;

		public async Task<bool> ShowMasterViewAsync<T>(string name, ViewsSwitchingAnimation animation, object eventParam = null) where T : View
		{
			T view = global::Xamarin.Forms.NameScopeExtensions.FindByName<T>(this, name);
			return await ShowMasterViewAsync((View)view, animation, eventParam);
		}

		public async Task<bool> ShowMasterViewAsync<T>(ViewsSwitchingAnimation animation, object eventParam = null)
		{
			foreach (var view in MasterViews)
			{
				if (view.GetType() == typeof(T))
					return await ShowMasterViewAsync(view, animation, eventParam);
			}

			return false;
		}

		public virtual async Task<bool> ShowMasterViewAsync(View newMasterView, ViewsSwitchingAnimation animation, object eventParam = null)
		{
			if (newMasterView == null || !MasterViews.Contains(newMasterView))
				return false;
			if (newMasterView == MasterView)
				return await ShowMasterViewAsync(animation, eventParam);

			MasterViewWillBeShown?.Invoke(this, (newMasterView, eventParam));

			View previousMasterView = MasterView;

			if (!Views.Contains(newMasterView))
				Views.Add(newMasterView);
			_ViewsLayout.RaiseChild(newMasterView);

			await AnimateAsync(true, previousMasterView, newMasterView, animation);

			MasterView = newMasterView;
			_MasterViewIsVisible = true;

			MasterViewHasBeenShown?.Invoke(this, (MasterView, eventParam));
			return true;
		}

		public virtual async Task<bool> ShowMasterViewAsync(ViewsSwitchingAnimation animation, object eventParam = null)
		{
			if (MasterView == null || IsSplitView || _MasterViewIsVisible)
				return false;

			MasterViewWillBeShown?.Invoke(this, (MasterView, eventParam));

			_ViewsLayout.RaiseChild(MasterView);

			await AnimateAsync(true, DetailView, MasterView, animation);

			_MasterViewIsVisible = true;
			MasterViewHasBeenShown?.Invoke(this, (MasterView, eventParam));
			return true;

		}

		public event EventHandler<(View view, object param)> MasterViewWillBeHidden;
		public event EventHandler<(View view, object param)> MasterViewHasBeenHidden;

		public virtual async Task<bool> HideMasterViewAsync(ViewsSwitchingAnimation animation, object eventParam = null)
		{
			if (MasterView == null || IsSplitView || !_MasterViewIsVisible)
				return false;

			MasterViewWillBeHidden?.Invoke(this, (MasterView, eventParam));

			_ViewsLayout.RaiseChild(DetailView);

			await AnimateAsync(true, MasterView, DetailView, animation);

			_MasterViewIsVisible = false;
			MasterViewHasBeenHidden?.Invoke(this, (MasterView, eventParam));
			return true;
		}


		//

		public event EventHandler<(View view, object param)> DetailViewWillBeShown;
		public event EventHandler<(View view, object param)> DetailViewHasBeenShown;

		public async Task<bool> ShowDetailViewAsync<T>(string name, ViewsSwitchingAnimation animation, object eventParam = null) where T : View
		{
			T view = global::Xamarin.Forms.NameScopeExtensions.FindByName<T>(this, name);
			return await ShowDetailViewAsync((View)view, animation, eventParam);
		}

		public async Task<bool> ShowDetailViewAsync<T>(ViewsSwitchingAnimation animation, object eventParam = null)
		{
			if (Views != null)
			{
				foreach (var view in Views)
				{
					if (view.GetType() == typeof(T))
						return await ShowDetailViewAsync(view, animation, eventParam);
				}
			}
			return false;
		}

		public virtual async Task<bool> ShowDetailViewAsync(View detailView, ViewsSwitchingAnimation animation, object eventParam = null)
		{
			if (detailView == null || Views == null || !Views.Contains(detailView))
				return false;

			DetailViewWillBeShown?.Invoke(this, (DetailView, eventParam));

			if (!_MasterViewIsVisible || IsSplitView)
			{
				View previousDetailView = DetailView;
				DetailView = detailView;

				_ViewsLayout.RaiseChild(DetailView);

				if (previousDetailView != DetailView)
					await AnimateAsync(false, previousDetailView, DetailView, animation);
#if WINDOWS_UWP
				else
				{
					DetailView.IsVisible = true;
				}
#endif
			}
			else
			{
				DetailView = detailView;
				await HideMasterViewAsync(animation);
			}

			DetailViewHasBeenShown?.Invoke(this, (DetailView, eventParam));
			return true;
		}


		//

		protected async Task AnimateAsync(bool mastersPane, View from, View to, ViewsSwitchingAnimation animation)
		{
#if WINDOWS_UWP
			animation = ViewsSwitchingAnimation.NoAnimation;
#endif

			BatchBegin();

			Rectangle vb = _ViewsLayout.Bounds;
			if (IsSplitView)
			{
				if (!mastersPane)
				{
					_ViewsLayout.RaiseChild(MasterView);
					vb = new Rectangle(MasterViewWidthInSplitView, 0, vb.Width - MasterViewWidthInSplitView, vb.Height);
				}
				else
				{
					if (DetailView != null)
						_ViewsLayout.RaiseChild(DetailView);
					vb = new Rectangle(0, 0, MasterViewWidthInSplitView, vb.Height);
				}
			}

#if WINDOWS_UWP
			to.IsVisible = true;
			from.IsVisible = true;
#endif

			Rectangle fromTo = SetupFromForAnimate(vb, from, animation);
			Rectangle toTo = SetupToForAnimate(vb, to, animation);

			if (animation != ViewsSwitchingAnimation.NoAnimation)
				await AnimateAsync(from, fromTo, to, toTo);

#if WINDOWS_UWP
			from.IsVisible = false;
#endif

			BatchCommit();
		}

		protected virtual Rectangle SetupFromForAnimate(Rectangle vb, View from, ViewsSwitchingAnimation animation)
		{
			Rectangle fromTo = vb;
			from.Layout(fromTo);
			if (animation != ViewsSwitchingAnimation.NoAnimation)
				if (animation == ViewsSwitchingAnimation.Forward)
					fromTo.X -= fromTo.Width;
				else
					fromTo.X += fromTo.Width;
			return fromTo;
		}

		protected virtual Rectangle SetupToForAnimate(Rectangle vb, View to, ViewsSwitchingAnimation animation)
		{
			Rectangle toTo = vb;
			if (animation != ViewsSwitchingAnimation.NoAnimation)
				if (animation == ViewsSwitchingAnimation.Forward)
					toTo.X += toTo.Width;
				else
					toTo.X -= toTo.Width;
			to.Layout(toTo);
			if (animation != ViewsSwitchingAnimation.NoAnimation)
			{
				if (animation == ViewsSwitchingAnimation.Forward)
					toTo.X -= toTo.Width;
				else
					toTo.X += toTo.Width;
			}
			return toTo;
		}

		protected virtual async Task AnimateAsync(View from, Rectangle fromTo, View to, Rectangle toTo)
		{
			await Task.WhenAll(
				from.LayoutTo(fromTo, DeviceEx.AnimationsLength, Easing.CubicOut),
				to.LayoutTo(toTo, DeviceEx.AnimationsLength, Easing.CubicOut)
			);
		}


		//

		Size _osa;

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);

			if (MasterView == null)
				return;
			if (!Tools.MakeIdenticalIfDifferent(new Size(width, height), ref _osa))
				return;

			void MasterViewsSetLayout(AbsoluteLayoutFlags flags, Rectangle bounds)
			{
				foreach (var view in MasterViews)
				{
					AbsoluteLayout.SetLayoutFlags(view, flags);
					AbsoluteLayout.SetLayoutBounds(view, bounds);
				}
			}

			void DetailViewsSetLayout(AbsoluteLayoutFlags flags, Rectangle bounds)
			{
				if (Views?.Count > 1)
				{
					for (int i = 0; i < Views.Count; i++)
					{
						View view = Views[i];
						if (!MasterViews.Contains(view))
						{
							AbsoluteLayout.SetLayoutFlags(view, flags);
							AbsoluteLayout.SetLayoutBounds(view, bounds);
						}
					}
				}
			}

			BatchBegin();

			void CalcMasterViewWidthInSplitView()
			{
				double rwidth = width;
				if (!IsSplitView)
					rwidth = Math.Max(width, height);

				MasterViewWidthInSplitView = DetailView != null ? Math.Max(MasterViewMinimumWidth, rwidth * MasterViewRelativeWidth) : width;

				if (rwidth < MasterViewMinimumWidth * 2)
					MasterViewWidthInSplitView = Math.Min(rwidth * 0.5, MasterViewWidthInSplitView);
			}

			if (!IsSplitView)
			{
				// Calculate master page width in split mode regardless of whether the device at this time is in landscape or portait
				CalcMasterViewWidthInSplitView();

				_MasterViewIsVisible = Views.IndexOf(MasterView) == Views.Count - 1;

				MasterViewsSetLayout(AbsoluteLayoutFlags.SizeProportional, new Rectangle(0, 0, 1, 1));
				DetailViewsSetLayout(AbsoluteLayoutFlags.SizeProportional, new Rectangle(0, 0, 1, 1));
			}
			else
			{
				CalcMasterViewWidthInSplitView();

				_MasterViewIsVisible = true;
#if WINDOWS_UWP
				MasterView.IsVisible = true;
				if(DetailView != null)
					DetailView.IsVisible = true;
#endif

				MasterViewsSetLayout(AbsoluteLayoutFlags.None, new Rectangle(0, 0, MasterViewWidthInSplitView, height));
				DetailViewsSetLayout(AbsoluteLayoutFlags.None, new Rectangle(MasterViewWidthInSplitView, 0, width - MasterViewWidthInSplitView, height));
			}

			ModalManager.OnSizeAllocated(width, height);

			BatchCommit();
		}
	}
}
