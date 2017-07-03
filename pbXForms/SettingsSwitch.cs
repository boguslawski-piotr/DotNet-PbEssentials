using System;
using Xamarin.Forms;

namespace pbXForms
{
	public class SettingsSwitch : SettingsControl
	{
		public static readonly BindableProperty IsToggledProperty =
			BindableProperty.Create("IsToggled",
									typeof(bool),
									typeof(SettingsSwitch),
									false,
									defaultBindingMode: BindingMode.TwoWay,
									propertyChanged: (bindable, oldValue, newValue) =>
									{
										((SettingsSwitch)bindable)._switch.IsToggled = (bool)newValue;
										((SettingsSwitch)bindable).Toggled?.Invoke(bindable, new ToggledEventArgs((bool)newValue));
									});

		public new bool IsEnabled
		{
			get => _switch.IsEnabled;
			set {
				_switch.IsEnabled = value;
				base.IsEnabled = value;
			}
		}

		public bool IsToggled
		{
			get => (bool)GetValue(IsToggledProperty);
			set => SetValue(IsToggledProperty, value);
		}

		public event EventHandler<ToggledEventArgs> Toggled;

		Switch _switch { get; set; }

		protected override void BuildControl()
		{
			base.BuildControl();

			_switch = new Switch();
			_switch.VerticalOptions = LayoutOptions.Center;
			_switch.Toggled += OnInnerSwitchToggled;

			ControlLayout.Children.Add(_switch);
		}

		protected virtual void OnInnerSwitchToggled(object sender, ToggledEventArgs e)
		{
			SetValue(IsToggledProperty, e.Value);
		}
	}
}
