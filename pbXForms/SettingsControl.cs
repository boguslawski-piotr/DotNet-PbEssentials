using Xamarin.Forms;

namespace pbXForms
{
	public class SettingsControl : StackLayout { }

	public class SettingsControlText : Label { }

	public class SettingsControlDesc : Label { }

	public class SettingsControlWithDesc : StackLayout
	{
		protected SettingsControl _ControlLayout;
		protected SettingsControlText _ControlText;

		protected SettingsControlDesc _Desc;
		protected SettingsLineSeparator _Separator;

		public string Text
		{
			get => _ControlText?.Text;
			set => _ControlText.Text = value;
		}

		public string Desc
		{
			get => _Desc?.Text;
			set {
				_Desc.Text = value;
				_Desc.IsVisible = true;
				_Separator.IsVisible = false;
			}
		}

		public bool Separator
		{
			get => _Separator.IsVisible;
			set => _Separator.IsVisible = value;
		}

		public new bool IsEnabled
		{
			get => _ControlText.IsEnabled;
			set {
				base.IsEnabled = value;

				_ControlLayout.IsEnabled = value;
				_ControlText.IsEnabled = value;
				_Desc.IsEnabled = value;

				_ControlText.Opacity = value ? 1 : 0.25;
				_Desc.Opacity = _ControlText.Opacity;
			}
		}

		public SettingsControlWithDesc()
		{
			Orientation = StackOrientation.Vertical;
			Padding = new Thickness(0);
			Margin = new Thickness(0);
			Spacing = 0;

			_ControlLayout = new SettingsControl()
			{
				Orientation = StackOrientation.Horizontal,
				Padding = new Thickness(Metrics.ButtonItemsSpacing, Metrics.ButtonItemsSpacing),
				Spacing = Metrics.ButtonItemsSpacing * 2,
			};

			_ControlText = new SettingsControlText()
			{
				FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
				HorizontalOptions = LayoutOptions.StartAndExpand,
				VerticalTextAlignment = TextAlignment.Center,
				Text = this.Text,
			};

			_ControlLayout.Children.Add(_ControlText);

			_Desc = new SettingsControlDesc()
			{
				FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
				Margin = new Thickness(Metrics.ButtonItemsSpacing, Metrics.ButtonItemsSpacing / 4, Metrics.ButtonItemsSpacing, Metrics.ButtonItemsSpacing * 2),
				IsVisible = false,
			};

			_Separator = new SettingsLineSeparator()
			{
				Margin = new Thickness(Metrics.ButtonItemsSpacing, 0, 0, 0),
			};

			Children.Add(_ControlLayout);
			Children.Add(_Desc);
			Children.Add(_Separator);
		}
	}
}