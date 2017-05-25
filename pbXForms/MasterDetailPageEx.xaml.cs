using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
    public class MasterDetailPageExViewsLayout : StackLayout
    {
        public MasterDetailPageExViewsLayout()
        {
            Orientation = StackOrientation.Horizontal;
            VerticalOptions = LayoutOptions.FillAndExpand;
            HorizontalOptions = LayoutOptions.FillAndExpand;
            Padding = new Thickness(0);
            Margin = new Thickness(0);
            Spacing = 0;
        }
    }

    public partial class MasterDetailPageEx : ContentPage
    {
        public IList<View> Views => _ViewsContainer?.Children;

        protected View _MasterView { get { return Views?.Count > 0 ? Views[0] : null; } }

        protected View _DetailView { get { return Views?.Count > 1 ? Views[1] : null; } }

        public NavigationEx NavigationEx = new NavigationEx();

        public MasterDetailPageEx()
        {
            InitializeComponent();
            NavigationEx.InitializeComponent(this, _ViewsContainer, _Blocker, _Dialog);
		}


        public virtual bool IsSplitView
        {
            //get { return !(Device.Idiom == TargetIdiom.Phone || (DeviceEx.Orientation == DeviceOrientations.Portrait)); }
            get { return DeviceEx.Orientation != DeviceOrientation.Portrait || Device.Idiom != TargetIdiom.Phone; }
        }


        bool _MasterViewIsVisible;
        public virtual bool MasterViewIsVisible
        {
            get {
                return _MasterViewIsVisible;
            }
            set {
                // TODO: add animation (slide in/out)
                if (_MasterView == null)
                    return;
                _MasterViewIsVisible = !IsSplitView ? value : true;
                _MasterView.IsVisible = _MasterViewIsVisible;
                if (_DetailView != null)
                    _DetailView.IsVisible = IsSplitView ? true : !_MasterViewIsVisible;
            }
        }

        public virtual double MasterViewMinimumWidth { get; set; } =
#if WINDOWS_UWP || __MACOS__
            320;
#else
            240;
#endif
        
        public virtual double MasterViewRelativeWidth { get; set; } = 0.3;


        Size _osa;

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (_MasterView == null)
                return;
            if (!Tools.IsDifferent(new Size(width, height), ref _osa))
                return;

            BatchBegin();

            if (!IsSplitView)
            {
                _MasterView.WidthRequest = width;
                if (_DetailView != null)
                    _DetailView.WidthRequest = width;
            }
            else
            {
                _MasterView.WidthRequest = Math.Max(MasterViewMinimumWidth, width * MasterViewRelativeWidth);
                if (_DetailView != null)
                    _DetailView.WidthRequest = width - _MasterView.WidthRequest;
            }

            _MasterView.IsVisible = IsSplitView ? true : MasterViewIsVisible;
            if (_DetailView != null)
                _DetailView.IsVisible = IsSplitView ? true : !MasterViewIsVisible;

            NavigationEx.OnSizeAllocated(width, height);

			BatchCommit();
        }

    }
}
