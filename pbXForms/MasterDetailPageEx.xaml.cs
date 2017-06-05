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
        public double MasterViewRelativeWidth { get; set; } = 0.3;
        public double MasterViewMinimumWidth { get; set; } =
#if WINDOWS_UWP || __MACOS__
            320;
#else
            240;
#endif
        public double MasterViewWidthInSplitView { get; protected set; }

        public IList<View> MasterViews { get; set; } = new List<View>();
        public IList<View> DetailViews => _ViewsLayout?.Children;

        protected View MasterView;
        protected View DetailView;

        protected IList<View> Views => _ViewsLayout?.Children;

        public ModalViewsManager ModalManager = new ModalViewsManager();

        public MasterDetailPageEx()
        {
            InitializeComponent();
            ModalManager.InitializeComponent(_Layout);
        }

        /// <summary>
        /// Initializes the views.
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
                _osa = new Size(-1, -1);
                ForceLayout();
            }
        }

        bool _MasterViewIsVisible = true;
        public virtual bool MasterViewIsVisible
        {
            get {
                return _MasterViewIsVisible;
            }
            set {
                if (value)
                    ShowMasterViewAsync(ViewsSwitchingAnimation.Back);
                else
                    HideMasterViewAsync(ViewsSwitchingAnimation.Forward);
            }
        }


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

            Rectangle fromTo = SetupFromForAnimate(vb, from, animation);
            Rectangle toTo = SetupToForAnimate(vb, to, animation);

            if (animation != ViewsSwitchingAnimation.NoAnimation)
                await AnimateAsync(from, fromTo, to, toTo);

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
            //Debug.WriteLine($"MasterDetailPageEx: OnSizeAllocated: w: {width}, h: {height}");

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
                        if (view != MasterView)
                        {
                            AbsoluteLayout.SetLayoutFlags(view, flags);
                            AbsoluteLayout.SetLayoutBounds(view, bounds);
                        }
                    }
                }
            }

            BatchBegin();

            if (!IsSplitView)
            {
                // Calculate master page width in split mode regardless of whether the device at this time is in landscape or portait
                MasterViewWidthInSplitView = DetailView != null ? Math.Max(MasterViewMinimumWidth, Math.Max(width, height) * MasterViewRelativeWidth) : width;

                _MasterViewIsVisible = Views.IndexOf(MasterView) == Views.Count - 1;

                MasterViewsSetLayout(AbsoluteLayoutFlags.SizeProportional, new Rectangle(0, 0, 1, 1));
                DetailViewsSetLayout(AbsoluteLayoutFlags.SizeProportional, new Rectangle(0, 0, 1, 1));
            }
            else
            {
                MasterViewWidthInSplitView = DetailView != null ? Math.Max(MasterViewMinimumWidth, width * MasterViewRelativeWidth) : width;

                _MasterViewIsVisible = true;

                MasterViewsSetLayout(AbsoluteLayoutFlags.None, new Rectangle(0, 0, MasterViewWidthInSplitView, height));
                DetailViewsSetLayout(AbsoluteLayoutFlags.None, new Rectangle(MasterViewWidthInSplitView, 0, width - MasterViewWidthInSplitView, height));
            }

            ModalManager.OnSizeAllocated(width, height);

            BatchCommit();
        }
    }
}
