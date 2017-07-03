using System;
using System.Collections.Generic;
using System.ComponentModel;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
	[EditorBrowsable(EditorBrowsableState.Never)]
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
		public Color GroupTextColor { get; set; }
		public Color GroupDescColor { get; set; }
		public FileImageSource GroupImage { get; set; }

		public Color GroupContentBackgroundColor { get; set; }

		public Color ControlBackgroundColor { get; set; }
		public Color ControlTextColor { get; set; }
		public Color ControlDescColor { get; set; }

		public Color SeparatorColor { get; set; }

		// TODO: fonts sizes (GroupText, GroupDesc, ControlText, ControlDesc)

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
				{
					if (e is Image i)
					{
						if (i.Source == null)
							i.Source = imageSource;
					}
				}
			}
		}

		protected override void OnParentSet()
		{
			base.OnParentSet();


			SetBackgroundColor(ViewContent, typeof(SettingsGroupHeaderLayout), GroupBackgroundColor);
			SetBackgroundColor(ViewContent, typeof(SettingsGroupInnerHeaderLayout), GroupBackgroundColor);
#if WINDOWS_UWP
			// Workaround bug(s) in TapGestureRecognizer. It doesn't work when background color is Default(Transparent).
			SetBackgroundColor(ViewContent, typeof(SettingsGroupHeaderLayout), BackgroundColor);
			SetBackgroundColor(ViewContent, typeof(SettingsGroupInnerHeaderLayout), BackgroundColor);
#endif
			SetTextColor(ViewContent, typeof(SettingsGroupText), GroupTextColor);
			SetTextColor(ViewContent, typeof(SettingsGroupDesc), GroupDescColor);
			SetImageSource(ViewContent, typeof(SettingsGroupImage), GroupImage);

			SetBackgroundColor(ViewContent, typeof(SettingsGroup), GroupContentBackgroundColor);

			SetBackgroundColor(ViewContent, typeof(SettingsControlLayout), ControlBackgroundColor);
			SetTextColor(ViewContent, typeof(SettingsControlText), ControlTextColor);
			SetTextColor(ViewContent, typeof(SettingsDesc), ControlDescColor);

			SetBackgroundColor(ViewContent, typeof(SettingsLineSeparator), SeparatorColor);
		}
	}
}
