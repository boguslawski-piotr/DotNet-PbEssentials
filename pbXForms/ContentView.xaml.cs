﻿using System.Collections.Generic;
using System.ComponentModel;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class ContentViewLayout : Grid
	{
		public ContentViewLayout()
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

	[EditorBrowsable(EditorBrowsableState.Never)]
	public class ContentViewViewLayout : StackLayout
	{
		public ContentViewViewLayout()
		{
			Orientation = StackOrientation.Vertical;
			VerticalOptions = LayoutOptions.FillAndExpand;
			HorizontalOptions = LayoutOptions.FillAndExpand;
			Padding = new Thickness(0);
			Margin = new Thickness(0);
			Spacing = 0;
		}
	}

	public partial class ContentView : BaseContentView
	{
		protected override AppBarLayout AppBarLayout => _AppBarRow;
		public IList<View> AppBarContent => _AppBarRow.Children;

		protected override Layout<View> ViewLayout => _ViewRow;
		public IList<View> ViewContent => _ViewRow.Children;

		protected override ToolBarLayout ToolBarLayout => _ToolBarRow;
		public IList<View> ToolBarContent => _ToolBarRow.Children;

		public ModalViewsManager ModalManager = new ModalViewsManager();

		protected override Grid ContentLayout => _ContentLayout;

		public ContentView()
		{
			InitializeComponent();
			ModalManager.InitializeComponent(_Layout);

			if (Device.Idiom != TargetIdiom.Desktop)
			{
				var panGesture = new PanGestureRecognizer();
				panGesture.PanUpdated += OnPanUpdated;
				_ContentLayout.GestureRecognizers.Add(panGesture);
			}
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);

			ModalManager.OnSizeAllocated(width, height);
		}

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
