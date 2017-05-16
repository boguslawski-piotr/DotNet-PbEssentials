using System;
using System.Collections.Generic;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
    public partial class MasterDetailPageEx : ContentPage
    {
        public IList<View> Views => _Views.Children;

        View _MasterView { get { return Views.Count > 0 ? Views[0] : null; } }

        View _DetailView { get { return Views.Count > 1 ? Views[1] : null; } }

        public MasterDetailPageEx()
        {
            InitializeComponent();
        }

        public virtual bool IsSplitView
        {
            //get { return !(Device.Idiom == TargetIdiom.Phone || (DeviceEx.Orientation == DeviceOrientations.Portrait)); }
            get { return DeviceEx.Orientation != DeviceOrientations.Portrait || Device.Idiom != TargetIdiom.Phone; }
        }

        bool _MasterViewIsVisible;
        public bool MasterViewIsVisible
        {
            get {
                return _MasterViewIsVisible;
            }
            set {
                _MasterViewIsVisible = !IsSplitView ? value : true;
                _MasterView.IsVisible = _MasterViewIsVisible;
                _DetailView.IsVisible = IsSplitView ? true : !_MasterViewIsVisible;
            }
        }

        //

        Size _osa;

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (!Tools.IsDifferent(new Size(width, height), ref _osa))
                return;

            BatchBegin();

            if (!IsSplitView)
            {
                _MasterView.WidthRequest = width;
                _DetailView.WidthRequest = width;
            }
            else
            {
                _MasterView.WidthRequest = Math.Max(240, width * 0.3);
                _DetailView.WidthRequest = width - _MasterView.WidthRequest;
            }

            _MasterView.IsVisible = IsSplitView ? true : MasterViewIsVisible;
            _DetailView.IsVisible = IsSplitView ? true : !MasterViewIsVisible;

            BatchCommit();
        }

    }
}
