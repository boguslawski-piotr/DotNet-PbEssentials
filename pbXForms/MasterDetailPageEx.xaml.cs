using System;
using System.Collections.Generic;
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
            if (_DetailView != null && showMasterView)
                _View.LowerChild(_DetailView);
            else
                _MasterViewIsVisible = false;
        }


        //

        public virtual bool IsSplitView
        {
            //get { return !(Device.Idiom == TargetIdiom.Phone || (DeviceEx.Orientation == DeviceOrientations.Portrait)); }
            get { return DeviceEx.Orientation != DeviceOrientation.Portrait || Device.Idiom != TargetIdiom.Phone; }
        }

        bool _MasterViewIsVisible = true;
        public virtual bool MasterViewIsVisible
        {
            get {
                return _MasterViewIsVisible;
            }
            set {
                if (_MasterView == null || IsSplitView)
                    return;

                _MasterViewIsVisible = value;
                if (_MasterViewIsVisible)
                {
                    ShowMasterView();
                }
                else
                {
                    HideMasterView();
                }
            }
        }

        protected virtual async Task ShowMasterView()
        {
			_View.LowerChild(_DetailView);

            Rectangle mto = _View.Bounds;
            mto.X -= mto.Width;
            _MasterView.Layout(mto);
            mto.X += mto.Width;

            Rectangle dto = _View.Bounds;
            _DetailView.Layout(dto);
			dto.X += dto.Width;

			await Task.WhenAny(
                _MasterView.LayoutTo(mto, ModalViewsManager.AnimationsLength, Easing.CubicOut),
                _DetailView.LayoutTo(dto, ModalViewsManager.AnimationsLength, Easing.CubicOut)
            );
		}
		
        protected virtual async Task HideMasterView()
		{
			Rectangle mto = _View.Bounds;
			_MasterView.Layout(mto);
			mto.X -= mto.Width;

			Rectangle dto = _View.Bounds;
			dto.X += dto.Width;
			_DetailView.Layout(dto);
            dto.X -= dto.Width;

			await Task.WhenAny(
				_MasterView.LayoutTo(mto, ModalViewsManager.AnimationsLength, Easing.CubicIn),
				_DetailView.LayoutTo(dto, ModalViewsManager.AnimationsLength, Easing.CubicIn)
			);
			
            _View.RaiseChild(_DetailView);
		}

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
				// Calculate master page width in split mode regardless of whether the device at this time is in landscape or portait
				MasterViewWidthInSplitView = _DetailView != null ? Math.Max(MasterViewMinimumWidth, Math.Max(width, height) * MasterViewRelativeWidth) : width;
				
                AbsoluteLayout.SetLayoutFlags(_MasterView, AbsoluteLayoutFlags.SizeProportional);
                AbsoluteLayout.SetLayoutBounds(_MasterView, new Rectangle(0, 0, 1, 1));
                if (_DetailView != null)
                {
                    AbsoluteLayout.SetLayoutFlags(_DetailView, AbsoluteLayoutFlags.SizeProportional);
                    AbsoluteLayout.SetLayoutBounds(_DetailView, new Rectangle(0, 0, 1, 1));
                }
            }
            else
            {
                MasterViewWidthInSplitView = _DetailView != null ? Math.Max(MasterViewMinimumWidth, width * MasterViewRelativeWidth) : width;

                AbsoluteLayout.SetLayoutFlags(_MasterView, AbsoluteLayoutFlags.None);
                AbsoluteLayout.SetLayoutBounds(_MasterView, new Rectangle(0, 0, MasterViewWidthInSplitView, height));
                if (_DetailView != null)
                {
                    AbsoluteLayout.SetLayoutFlags(_DetailView, AbsoluteLayoutFlags.None);
                    AbsoluteLayout.SetLayoutBounds(_DetailView, new Rectangle(MasterViewWidthInSplitView, 0, width - MasterViewWidthInSplitView, height));
                }
            }

			ModalViewsManager.OnSizeAllocated(width, height);

            BatchCommit();
        }
    }
}
