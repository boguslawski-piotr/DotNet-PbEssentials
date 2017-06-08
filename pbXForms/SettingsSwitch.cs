using System;
using Xamarin.Forms;

namespace pbXForms
{
	public class SettingsSwitch : SettingsControlWithDesc
	{
		public static readonly BindableProperty IsToggledProperty =
			BindableProperty.Create("IsToggled",
									typeof(bool),
									typeof(SettingsSwitch),
									false,
									defaultBindingMode: BindingMode.TwoWay,
									propertyChanged: (bindable, oldValue, newValue) =>
									{
										((SettingsSwitch)bindable)._Switch.IsToggled = (bool)newValue;
										((SettingsSwitch)bindable).Toggled?.Invoke(bindable, new ToggledEventArgs((bool)newValue));
									});

		public new bool IsEnabled
		{
			get => _Switch.IsEnabled;
			set {
				_Switch.IsEnabled = value;
				base.IsEnabled = value;
			}
		}

		public bool IsToggled
		{
			get => (bool)GetValue(IsToggledProperty);
			set => SetValue(IsToggledProperty, value);
		}

		public event EventHandler<ToggledEventArgs> Toggled;

		Switch _Switch { get; set; }

		public SettingsSwitch()
		{
			_Switch = new Switch();
			_Switch.VerticalOptions = LayoutOptions.Center;
			_Switch.Toggled += OnInnerSwitchToggled;

			_Control.Children.Add(_Switch);
		}

		protected virtual void OnInnerSwitchToggled(object sender, ToggledEventArgs e)
		{
			SetValue(IsToggledProperty, e.Value);
		}
	}
}
