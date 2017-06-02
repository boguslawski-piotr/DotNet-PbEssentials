using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
    public class ContentViewExLayout : Grid
    {
        public ContentViewExLayout()
        {
            Padding = new Thickness(0);
            Margin = new Thickness(0);
            ColumnSpacing = 0;
            RowSpacing = 0;

            ColumnDefinitions = new ColumnDefinitionCollection() {
                    new ColumnDefinition() {
                        Width = GridLength.Star,
                    },
                };

            RowDefinitions = new RowDefinitionCollection() {
                    new RowDefinition() {
                        Height = new GridLength(0)
                    },
                    new RowDefinition() {
                        Height = GridLength.Star
                    },
                    new RowDefinition() {
                        Height = new GridLength(0)
                    },
                };
        }
    }

    public class ContentViewExContentLayout : StackLayout
    {
        public ContentViewExContentLayout()
        {
            Orientation = StackOrientation.Vertical;
            VerticalOptions = LayoutOptions.FillAndExpand;
            HorizontalOptions = LayoutOptions.FillAndExpand;
            Padding = new Thickness(0);
            Margin = new Thickness(0);
            Spacing = 0;
        }
    }


    public partial class ContentViewEx : ModalContentView
    {
        public AppBarLayout AppBar => _AppBarRow;
        public IList<View> AppBarContent => _AppBarRow.Children;

        public IList<View> ViewContent => _ContentRow.Children;

        public ToolBarLayout ToolBar => _ToolBarRow;
        public IList<View> ToolBarContent => _ToolBarRow.Children;

        public ModalViewsManager ModalManager = new ModalViewsManager();

        public bool ViewCoversStatusBar => Device.RuntimePlatform == Device.iOS ? true : false;

        public ContentViewEx()
        {
            InitializeComponent();
            ModalManager.InitializeComponent(_Layout);

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            _ContentLayout.GestureRecognizers.Add(panGesture);
        }


        //

        Size _osa;

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (_ContentLayout == null)
                return;
            if (!Tools.IsDifferent(new Size(width, height), ref _osa))
                return;

            BatchBegin();

            LayoutAppBarAndToolBar(width, height);

            ContinueOnSizeAllocated(width, height);

            ModalManager.OnSizeAllocated(width, height);

            BatchCommit();
        }

        protected virtual void LayoutAppBarAndToolBar(double width, double height)
        {
            if (_ContentLayout == null)
                return;

            bool IsLandscape = (DeviceEx.Orientation == DeviceOrientation.Landscape);

            if (_AppBarRow?.Children?.Count > 0)
            {
                _ContentLayout.RowDefinitions[0].Height =
                    (IsLandscape ? Metrics.AppBarHeightLandscape : Metrics.AppBarHeightPortrait)
                    + ((ViewCoversStatusBar && DeviceEx.StatusBarVisible) ? Metrics.StatusBarHeight : 0);

                _AppBarRow.Padding = new Thickness(
                    0,
                    (ViewCoversStatusBar && DeviceEx.StatusBarVisible ? Metrics.StatusBarHeight : 0),
                    0,
                    0);
            }

            if (_ToolBarRow?.Children?.Count > 0)
                _ContentLayout.RowDefinitions[2].Height = (IsLandscape ? Metrics.ToolBarHeightLandscape : Metrics.ToolBarHeightPortrait);
        }

        protected virtual void ContinueOnSizeAllocated(double width, double height)
        {
        }


        //

        double swipeLength = 0;

        void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (e.StatusType == GestureStatus.Started)
                swipeLength = 0;
            if (e.StatusType == GestureStatus.Running)
                swipeLength = e.TotalX;
            if (e.StatusType == GestureStatus.Completed)
            {
                double swipeMinLength = _ContentLayout.Bounds.Width / 4;
                if (swipeLength > swipeMinLength)
                    OnSwipeLeftToRight();
                else if (swipeLength < 0 && swipeLength * -1 > swipeMinLength)
                    OnSwipeRightToLeft();

                swipeLength = 0;
            }
        }

        public virtual void OnSwipeRightToLeft()
        {
        }

        public virtual void OnSwipeLeftToRight()
        {
        }
    }
}
