using Xamarin.Forms;

namespace pbXForms
{
	public class SettingsControl : StackLayout
	{
	}

	public class SettingsControlText : Label
	{
	}

	public class SettingsControlDesc : Label
	{
	}

	public class SettingsControlWithDesc : StackLayout
	{
		protected SettingsControl _Control;

		SettingsControlText _ControlText;
		SettingsControlDesc _ControlDesc;
		SettingsLineSeparator _Separator;

		public string Text
		{
			get => _ControlText?.Text;
			set => _ControlText.Text = value;
		}

		public string Desc
		{
			get => _ControlDesc?.Text;
			set
			{
				_ControlDesc.Text = value;
				_ControlDesc.IsVisible = true;
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
			set
			{
				base.IsEnabled = value;

				_Control.IsEnabled = value;
				_ControlText.IsEnabled = value;
				_ControlDesc.IsEnabled = value;

				_ControlText.Opacity = value ? 1 : 0.25;
				_ControlDesc.Opacity = _ControlText.Opacity;
			}
		}

		public SettingsControlWithDesc()
		{
			Orientation = StackOrientation.Vertical;
			Padding = new Thickness(0);
			Margin = new Thickness(0);
			Spacing = 0;

			_Control = new SettingsControl()
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

			_Control.Children.Add(_ControlText);

			_ControlDesc = new SettingsControlDesc()
			{
				FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
				Margin = new Thickness(Metrics.ButtonItemsSpacing, Metrics.ButtonItemsSpacing / 4, Metrics.ButtonItemsSpacing, Metrics.ButtonItemsSpacing * 2),
				IsVisible = false,
			};

			_Separator = new SettingsLineSeparator()
			{
				Margin = new Thickness(Metrics.ButtonItemsSpacing, 0, 0, 0),
			};

			Children.Add(_Control);
			Children.Add(_ControlDesc);
			Children.Add(_Separator);
		}
	}
}