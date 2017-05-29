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

        public virtual async Task ShowMasterViewAsync()
        {
            if (_MasterView == null || IsSplitView || _MasterViewIsVisible)
                return;

            _View.RaiseChild(_MasterView);

            await AnimateAsync(_DetailView, _MasterView, AnimateMasterViewDuringShowHide ? ViewsSwitchingAnimation.LeftToRight : ViewsSwitchingAnimation.NoAnimation);

            _MasterViewIsVisible = true;
        }

        public virtual async Task HideMasterViewAsync()
        {
            if (_MasterView == null || IsSplitView || !_MasterViewIsVisible)
                return;

            _View.RaiseChild(_DetailView);

            await AnimateAsync(_MasterView, _DetailView, AnimateMasterViewDuringShowHide ? ViewsSwitchingAnimation.RightToLeft : ViewsSwitchingAnimation.NoAnimation);

            _MasterViewIsVisible = false;
        }

        public virtual async Task<bool> ShowDetailViewAsync<T>(string name, ViewsSwitchingAnimation animation) where T : View
        {
            T view = global::Xamarin.Forms.NameScopeExtensions.FindByName<T>(this, name);
            return await ShowDetailViewAsync((View)view, animation);
        }

        public virtual async Task<bool> ShowDetailViewAsync<T>(ViewsSwitchingAnimation animation)
        {
            foreach (var view in Views)
            {
                if (view.GetType() == typeof(T))
                    return await ShowDetailViewAsync(view, animation);
            }

            return false;
        }

        public virtual async Task<bool> ShowDetailViewAsync(View detailView, ViewsSwitchingAnimation animation)
        {
            if (detailView == null || Views == null || !Views.Contains(detailView))
                return false;

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
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			from.LayoutTo(fromTo, DeviceEx.AnimationsLength, easing);
            to.LayoutTo(toTo, DeviceEx.AnimationsLength, easing);
#pragma warning restore CS4014
			//await Task.WhenAll(
			//    from.LayoutTo(fromTo, DeviceEx.AnimationsLength, easing),
			//    to.LayoutTo(toTo, DeviceEx.AnimationsLength, easing)
			//);
		}

        Size _osa;

        protected override void OnSizeAllocated(double width, double height)
        {
            Debug.WriteLine($"MasterDetailPageEx: OnSizeAllocated: w: {width}, h: {height}");

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
