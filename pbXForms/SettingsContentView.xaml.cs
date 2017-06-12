using System;
using System.Collections.Generic;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
	public class SettingsContentViewLayout : Grid
	{
		public SettingsContentViewLayout()
		{
			Padding = new Thickness(0);
			Margin = new Thickness(0);
			ColumnSpacing = 0;
			RowSpacing = 0;

			ColumnDefinitions = new ColumnDefinitionCollection() {
					new ColumnDefinition() { Width = GridLength.Star },
				};

			RowDefinitions = new RowDefinitionCollection() {
					new RowDefinition() { Height = GridLength.Auto },
					new RowDefinition() { Height = GridLength.Star },
					new RowDefinition() { Height = GridLength.Auto },
				};
		}
	}

	public partial class SettingsContentView : ModalContentView
	{
		public Color GroupBackgroundColor { get; set; }
		public Color GroupHeaderBackgroundColor { get; set; }
		public Color GroupTextTextColor { get; set; }
		public Color GroupDescTextColor { get; set; }
		public FileImageSource GroupCollapsedExpandedImage { get; set; }

		public Color ControlBackgroundColor { get; set; }
		public Color ControlTextTextColor { get; set; }
		public Color ControlDescTextColor { get; set; }

		protected override AppBarLayout AppBarLayout => _AppBarRow;
		public IList<View> AppBarContent => _AppBarRow.Children;

		protected override Layout<View> ViewLayout => _ViewRow;
		public IList<View> ViewContent => _ViewRow.Children;

		protected override ToolBarLayout ToolBarLayout => _ToolBarRow;
		public IList<View> ToolBarContent => _ToolBarRow.Children;

		protected override Grid ContentLayout => _ContentLayout;

		public SettingsContentView()
		{
			InitializeComponent();
			_ViewRowScroller.Padding = new Thickness(0, 0, 0, Metrics.ButtonItemsSpacing * 3);
		}

		void SetColor(IList<View> l, Type type, Color color, Action<VisualElement, Color> setter)
		{
			if (color != Color.Default)
			{
				foreach (var e in l)
				{
					if (e is Layout<View> sl)
						SetColor(sl.Children, type, color, setter);

					if (e.GetType() == type)
						setter(e, color);
				}
			}
		}

		void SetBackgroundColor(IList<View> l, Type type, Color color)
		{
			SetColor(l, type, color, (e, c) =>
			{
				if (e.BackgroundColor == Color.Default)
					e.BackgroundColor = c;
			});
		}

		void SetTextColor(IList<View> l, Type type, Color color)
		{
			SetColor(l, type, color, (e, c) =>
			{
				if (e is Label le)
				{
					if (le.TextColor == Color.Default)
						le.TextColor = c;
				}
			});
		}

		void SetImageSource(IList<View> l, Type type, FileImageSource imageSource)
		{
			foreach (var e in l)
			{
				if (e is Layout<View> sl)
					SetImageSource(sl.Children, type, imageSource);

				if (e.GetType() == type)
					(e as Image).Source = imageSource;
			}
		}

		protected override void OnParentSet()
		{
			base.OnParentSet();

			SetBackgroundColor(ViewContent, typeof(SettingsGroup), GroupBackgroundColor);
			SetBackgroundColor(ViewContent, typeof(SettingsGroupHeader), GroupHeaderBackgroundColor);
			SetTextColor(ViewContent, typeof(SettingsGroupText), GroupTextTextColor);
			SetTextColor(ViewContent, typeof(SettingsGroupDesc), GroupDescTextColor);
			SetImageSource(ViewContent, typeof(SettingsGroupCollapsedExpandedImage), GroupCollapsedExpandedImage);

			SetBackgroundColor(ViewContent, typeof(SettingsControl), ControlBackgroundColor);
			SetTextColor(ViewContent, typeof(SettingsControlText), ControlTextTextColor);
			SetTextColor(ViewContent, typeof(SettingsControlDesc), ControlDescTextColor);
		}
	}
}
