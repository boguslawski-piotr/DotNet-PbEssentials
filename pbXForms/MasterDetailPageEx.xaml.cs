using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
    public class MasterDetailPageExViewsLayout : AbsoluteLayout
    {
    }

    public partial class MasterDetailPageEx : ContentPage
    {
        public virtual double MasterViewRelativeWidth { get; set; } = 0.3;
        public virtual double MasterViewMinimumWidth { get; set; } =
#if WINDOWS_UWP || __MACOS__
            320;
#else
            240;
#endif
        public virtual bool AnimateMasterViewDuringShowHide { get; set; } = true;

        public virtual double MasterViewActualWidth { get => (_MasterView == null ? 0 : _MasterView.Bounds.Width); }
        public virtual double MasterViewWidthInSplitView { get; protected set; }

        public IList<View> Views => _View?.Children;

        protected View _MasterView;
        protected View _DetailView;

        public ModalViewsManager ModalViewsManager = new ModalViewsManager();

        public MasterDetailPageEx()
        {
            InitializeComponent();
            ModalViewsManager.InitializeComponent(_Layout);
        }

        /// <summary>
        /// Initializes the views.
        /// Must be called AFTER InitializeComponent() of a class that inherits from this.
        /// </summary>
        public virtual void InitializeViews(bool showMasterView = true)
        {
            if (Views?.Count <= 0)
                return;

            _MasterView = Views[0];
            _DetailView = Views?.Count > 1 ? Views[1] : null;

            _MasterViewIsVisible = !IsSplitView;
            if (_DetailView != null)
                _View.RaiseChild(_DetailView);
            if (showMasterView)
                _View.RaiseChild(_MasterView);

            // TODO: events willBe... hasBeen... for detail and master ?
        }


        //

        int _IsSplitView = -1;
        public virtual bool IsSplitView
        {
            //get { return !(Device.Idiom == TargetIdiom.Phone || (DeviceEx.Orientation == DeviceOrientations.Portrait)); }
            get {
                if (_IsSplitView > -1)
                    return _IsSplitView == 1;

                return DeviceEx.Orientation != DeviceOrientation.Portrait /*|| Device.Idiom == TargetIdiom.Desktop*/;
            }
            set {
                _IsSplitView = value ? 1 : 0;
                _osa = new Size(-1,-1);
                ForceLayout();
            }
        }

        public enum ViewsSwitchingAnimation
        {
            NoAnimation,
            RightToLeft,
            LeftToRight,
        };

        bool _MasterViewIsVisible = true;
        public virtual bool MasterViewIsVisible
        {
            get {
                return _MasterViewIsVisible;
            }
            set {
                if (value)
                    ShowMasterViewAsync();
                else
                    HideMasterViewAsync();
            }
        }

        public event EventHandler<(View view, object param)> MasterViewWillBeShown;
		public event EventHandler<(View view, object param)> MasterViewHasBeenShown;

		public virtual async Task ShowMasterViewAsync(object eventParam = null)
        {
            if (_MasterView == null || IsSplitView || _MasterViewIsVisible)
                return;

			MasterViewWillBeShown?.Invoke(this, (_MasterView, eventParam));

			_View.RaiseChild(_MasterView);

            await AnimateAsync(_DetailView, _MasterView, AnimateMasterViewDuringShowHide ? ViewsSwitchingAnimation.LeftToRight : ViewsSwitchingAnimation.NoAnimation);

            _MasterViewIsVisible = true;
			MasterViewHasBeenShown?.Invoke(this, (_MasterView, eventParam));
		}

		public event EventHandler<(View view, object param)> MasterViewWillBeHidden;
		public event EventHandler<(View view, object param)> MasterViewHasBeenHidden;

		public virtual async Task HideMasterViewAsync(object eventParam = null)
        {
            if (_MasterView == null || IsSplitView || !_MasterViewIsVisible)
                return;

			MasterViewWillBeHidden?.Invoke(this, (_MasterView, eventParam));

			_View.RaiseChild(_DetailView);

            await AnimateAsync(_MasterView, _DetailView, AnimateMasterViewDuringShowHide ? ViewsSwitchingAnimation.RightToLeft : ViewsSwitchingAnimation.NoAnimation);

            _MasterViewIsVisible = false;
			MasterViewHasBeenHidden?.Invoke(this, (_MasterView, eventParam));
		}

        public virtual async Task<bool> ShowDetailViewAsync<T>(string name, ViewsSwitchingAnimation animation, object eventParam = null) where T : View
        {
            T view = global::Xamarin.Forms.NameScopeExtensions.FindByName<T>(this, name);
            return await ShowDetailViewAsync((View)view, animation, eventParam);
        }

        public virtual async Task<bool> ShowDetailViewAsync<T>(ViewsSwitchingAnimation animation, object eventParam = null)
        {
            foreach (var view in Views)
            {
                if (view.GetType() == typeof(T))
                    return await ShowDetailViewAsync(view, animation, eventParam);
            }

            return false;
        }

		public event EventHandler<(View view, object param)> DetailViewWillBeShown;
		public event EventHandler<(View view, object param)> DetailViewHasBeenShown;

        public virtual async Task<bool> ShowDetailViewAsync(View detailView, ViewsSwitchingAnimation animation, object eventParam = null)
        {
            if (detailView == null || Views == null || !Views.Contains(detailView))
                return false;

            DetailViewWillBeShown?.Invoke(this, (_DetailView, eventParam));

            if (!_MasterViewIsVisible || IsSplitView)
            {
                View previousDetailView = _DetailView;
                _DetailView = detailView;

                _View.RaiseChild(_DetailView);

                if (previousDetailView != _DetailView)
                    await AnimateAsync(previousDetailView, _DetailView, animation);
            }
            else
            {
                _DetailView = detailView;
                await HideMasterViewAsync();
            }

            DetailViewHasBeenShown?.Invoke(this, (_DetailView, eventParam));
            return true;
        }

        protected virtual async Task AnimateAsync(View from, View to, ViewsSwitchingAnimation animation)
        {
            Rectangle vb = _View.Bounds;
            if (IsSplitView)
            {
                // Animate only detail pages in split view
                _View.RaiseChild(_MasterView);
                vb = new Rectangle(MasterViewWidthInSplitView, 0, vb.Width - MasterViewWidthInSplitView, vb.Height);
            }

            Rectangle fromTo = vb;
            from.Layout(fromTo);
            if (animation != ViewsSwitchingAnimation.NoAnimation)
                if (animation == ViewsSwitchingAnimation.RightToLeft)
                    fromTo.X -= fromTo.Width;
                else
                    fromTo.X += fromTo.Width;

            Rectangle toTo = vb;
            if (animation != ViewsSwitchingAnimation.NoAnimation)
                if (animation == ViewsSwitchingAnimation.RightToLeft)
                    toTo.X += toTo.Width;
                else
                    toTo.X -= toTo.Width;
            to.Layout(toTo);
            if (animation != ViewsSwitchingAnimation.NoAnimation)
            {
                if (animation == ViewsSwitchingAnimation.RightToLeft)
                    toTo.X -= toTo.Width;
                else
                    toTo.X += toTo.Width;

                await AnimateAsync(from, fromTo, to, toTo, animation != ViewsSwitchingAnimation.RightToLeft ? Easing.CubicOut : Easing.CubicIn);
            }
        }

        protected virtual async Task AnimateAsync(View from, Rectangle fromTo, View to, Rectangle toTo, Easing easing)
        {
			await Task.WhenAll(
			    from.LayoutTo(fromTo, DeviceEx.AnimationsLength, easing),
			    to.LayoutTo(toTo, DeviceEx.AnimationsLength, easing)
			);
		}

        Size _osa;

        protected override void OnSizeAllocated(double width, double height)
        {
            //Debug.WriteLine($"MasterDetailPageEx: OnSizeAllocated: w: {width}, h: {height}");

            base.OnSizeAllocated(width, height);

            if (_MasterView == null)
                return;
            if (!Tools.IsDifferent(new Size(width, height), ref _osa))
                return;

            BatchBegin();

            if (!IsSplitView)
            {
                // Calculate master page width in split mode regardless of whether the device at this time is in landscape or portait
                MasterViewWidthInSplitView = _DetailView != null ? Math.Max(MasterViewMinimumWidth, Math.Max(width, height) * MasterViewRelativeWidth) : width;
                _MasterViewIsVisible = Views.IndexOf(_MasterView) == Views.Count - 1;

                AbsoluteLayout.SetLayoutFlags(_MasterView, AbsoluteLayoutFlags.SizeProportional);
                AbsoluteLayout.SetLayoutBounds(_MasterView, new Rectangle(0, 0, 1, 1));
                if (Views?.Count > 1)
                {
                    for (int i = 0; i < Views.Count; i++)
                    {
                        View view = Views[i];
                        if (view != _MasterView)
                        {
                            AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.SizeProportional);
                            AbsoluteLayout.SetLayoutBounds(view, new Rectangle(0, 0, 1, 1));
                        }
                    }
                }
            }
            else
            {
                MasterViewWidthInSplitView = _DetailView != null ? Math.Max(MasterViewMinimumWidth, width * MasterViewRelativeWidth) : width;
                _MasterViewIsVisible = true;

                AbsoluteLayout.SetLayoutFlags(_MasterView, AbsoluteLayoutFlags.None);
                AbsoluteLayout.SetLayoutBounds(_MasterView, new Rectangle(0, 0, MasterViewWidthInSplitView, height));
                if (Views?.Count > 1)
                {
                    for (int i = 0; i < Views.Count; i++)
                    {
                        View view = Views[i];
                        if (view != _MasterView)
                        {
                            AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.None);
                            AbsoluteLayout.SetLayoutBounds(view, new Rectangle(MasterViewWidthInSplitView, 0, width - MasterViewWidthInSplitView, height));
                        }
                    }
                }
            }

            ModalViewsManager.OnSizeAllocated(width, height);

            BatchCommit();
        }
    }
}
