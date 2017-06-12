using System;
using Xamarin.Forms;

namespace pbXForms
{
	public class SettingsSeparator : BoxView
	{
		public SettingsSeparator()
		{
			HeightRequest = Metrics.ButtonItemsSpacing * 2;
			BackgroundColor = Color.Transparent;
		}
	}

	public class SettingsLineSeparator : BoxView
	{
		public SettingsLineSeparator()
		{
			HeightRequest = 0.5;
			BackgroundColor = Color.Black;
			Opacity = 0.15;
		}
	}
}
