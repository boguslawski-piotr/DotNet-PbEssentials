using System.ComponentModel;
using Xamarin.Forms;

namespace pbXForms
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class SettingsControlLayout : StackLayout { }

	[EditorBrowsable(EditorBrowsableState.Never)]
	public class SettingsControlText : Label { }

	[EditorBrowsable(EditorBrowsableState.Never)]
	public class SettingsDesc : Label { }

	public class SettingsControl : StackLayout
	{
		protected SettingsControlLayout ControlLayout;
		protected SettingsControlText ControlText;

		protected SettingsDesc Desc;
		protected SettingsLineSeparator Separator;

		public string Text
		{
			get => ControlText?.Text;
			set => ControlText.Text = value;
		}

		public string Description
		{
			get => Desc.Text;
			set {
				Desc.Text = value;
				Desc.IsVisible = !string.IsNullOrWhiteSpace(value);
				Separator.IsVisible = !Desc.IsVisible;
			}
		}

		public bool SeparatorIsVisible
		{
			get => Separator.IsVisible;
			set => Separator.IsVisible = value;
		}

		public new bool IsEnabled
		{
			get => ControlText.IsEnabled;
			set {
				base.IsEnabled = value;

				ControlLayout.IsEnabled = value;
				ControlText.IsEnabled = value;
				Desc.IsEnabled = value;

				ControlText.Opacity = value ? 1 : 0.25;
				Desc.Opacity = ControlText.Opacity;
			}
		}

		public SettingsControl()
		{
			Build();
		}

		protected virtual void Build()
		{
			Orientation = StackOrientation.Vertical;
			Padding = new Thickness(0);
			Margin = new Thickness(0);
			Spacing = 0;

			BuildControl();
			BuildDesc();
			BuildSeparator();

			Children.Add(ControlLayout);
			Children.Add(Desc);
			Children.Add(Separator);
		}

		protected virtual void BuildControl()
		{
			ControlLayout = new SettingsControlLayout()
			{
				Orientation = StackOrientation.Horizontal,
				Padding = new Thickness(Metrics.ButtonItemsSpacing, Metrics.ButtonItemsSpacing),
				Spacing = Metrics.ButtonItemsSpacing * 2,
			};

			ControlText = new SettingsControlText()
			{
				FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
				HorizontalOptions = LayoutOptions.StartAndExpand,
				VerticalTextAlignment = TextAlignment.Center,
				Text = this.Text,
			};

			ControlLayout.Children.Add(ControlText);
		}

		protected virtual void BuildDesc()
		{
			Desc = new SettingsDesc()
			{
				FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
				Margin = new Thickness(Metrics.ButtonItemsSpacing, Metrics.ButtonItemsSpacing / 4, Metrics.ButtonItemsSpacing, Metrics.ButtonItemsSpacing * 2),
				IsVisible = false,
			};
		}

		protected virtual void BuildSeparator()
		{
			Separator = new SettingsLineSeparator()
			{
				Margin = new Thickness(Metrics.ButtonItemsSpacing, 0, 0, 0),
			};
		}
	}
}