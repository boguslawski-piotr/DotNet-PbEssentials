using System;
using Xamarin.Forms;

namespace pbXForms
{
	public class SettingsSwitch : SettingsControlWithDesc
	{
		public static readonly BindableProperty IsToggledProperty = BindableProperty.Create("IsToggled", typeof(bool), typeof(SettingsSwitch), false, BindingMode.TwoWay);

		public new bool IsEnabled
		{
			get => _Switch.IsEnabled;
			set
			{
				_Switch.IsEnabled = value;
				base.IsEnabled = value;
			}
		}

		public bool IsToggled
		{
			get => (bool)GetValue(IsToggledProperty);
			set
			{
				if (IsToggled != value)
				{
					SetValue(IsToggledProperty, value);
					_Switch.IsToggled = value;
				}
			}
		}

		public event EventHandler<ToggledEventArgs> Toggled;

		Switch _Switch { get; set; }

		public SettingsSwitch()
		{
			_Switch = new Switch();
			_Switch.VerticalOptions = LayoutOptions.Center;

			_Control.Children.Add(_Switch);
		}

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();

			_Switch.Toggled -= OnToggled;
			_Switch.IsToggled = this.IsToggled;
			_Switch.Toggled += OnToggled;
		}

		protected virtual void OnToggled(object sender, ToggledEventArgs e)
		{
			SetValue(IsToggledProperty, e.Value);
			Toggled?.Invoke(sender, e);
		}
	}
}
