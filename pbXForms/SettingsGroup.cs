using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace pbXForms
{
	public class SettingsGroupHeader : StackLayout
	{
	}

	public class SettingsGroupText : Label
	{
	}

	public class SettingsGroupDesc : Label
	{
	}

	public class SettingsGroupContent : StackLayout
	{
	}

	public class SettingsGroupCollapsedExpandedImage : Image { }

	[ContentProperty("_GroupContent")]
	public class SettingsGroup : StackLayout
	{
		SettingsGroupHeader _HeaderFrame;

		SettingsGroupHeader _Header;
		SettingsGroupText _HeaderText;
		SettingsGroupDesc _HeaderDesc;

		SettingsGroupCollapsedExpandedImage _HeaderCollapsedExpandedImage;

		SettingsGroupContent _Content;
		IList<View> _GroupContent => _Content.Children;

		SettingsLineSeparator _Separator;

		public string Text
		{
			get => _HeaderText?.Text;
			set => _HeaderText.Text = value;
		}

		public string Desc
		{
			get => _HeaderDesc?.Text;
			set
			{
				_HeaderDesc.Text = value;
				_HeaderDesc.IsVisible = true;
			}
		}

		public bool Separator {
			get => _Separator.IsVisible;
			set => _Separator.IsVisible = value;
		}

		public bool IsCollapsed
		{
			get => !_Content.IsVisible;
			set
			{
				_Content.IsVisible = !value;
				_HeaderCollapsedExpandedImage.Rotation = value ? 0 : 90;
			}
		}

		public SettingsGroup()
		{
			Orientation = StackOrientation.Vertical;
			Padding = new Thickness(0);
			Margin = new Thickness(0);
			Spacing = 0;

			_HeaderFrame = new SettingsGroupHeader()
			{
				Orientation = StackOrientation.Horizontal,
				Spacing = Metrics.ButtonItemsSpacing,
				Padding = new Thickness(0, 0, Metrics.ButtonItemsSpacing, 0),
				Margin = new Thickness(0)
			};

			TapGestureRecognizer tgr = new TapGestureRecognizer();
			tgr.Command = new Command(OnTapped);
			_HeaderFrame.GestureRecognizers.Add(tgr);

			_Header = new SettingsGroupHeader()
			{
				Orientation = StackOrientation.Vertical,
				HorizontalOptions = LayoutOptions.StartAndExpand,
				Spacing = Metrics.ButtonItemsSpacing / 4,
				Padding = new Thickness(Metrics.ButtonItemsSpacing, Metrics.ButtonItemsSpacing * 3, Metrics.ButtonItemsSpacing, Metrics.ButtonItemsSpacing * 2),
				Margin = new Thickness(0)
			};

			_HeaderText = new SettingsGroupText()
			{
				FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
				HorizontalOptions = LayoutOptions.StartAndExpand,
			};

			_HeaderDesc = new SettingsGroupDesc()
			{
				FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
				IsVisible = false,
			};

			_Header.Children.Add(_HeaderText);
			_Header.Children.Add(_HeaderDesc);

			_HeaderCollapsedExpandedImage = new SettingsGroupCollapsedExpandedImage();

			_HeaderFrame.Children.Add(_Header);
			_HeaderFrame.Children.Add(_HeaderCollapsedExpandedImage);

			_Content = new SettingsGroupContent()
			{
				Orientation = StackOrientation.Vertical,
				Padding = new Thickness(0),
				Margin = new Thickness(0),
				Spacing = 0,
			};

			_Separator = new SettingsLineSeparator();

			Children.Add(_HeaderFrame);
			Children.Add(_Content);
			Children.Add(_Separator);
		}

		async void OnTapped(object parameter)
		{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			_HeaderCollapsedExpandedImage.RotateTo(IsCollapsed ? 90 : 0, DeviceEx.AnimationsLength);
#pragma warning restore CS4014

			await _Header.FadeTo(0.25, (uint)(DeviceEx.AnimationsLength * 0.50), Easing.CubicOut);

			_Content.IsVisible = !_Content.IsVisible;

			await _Header.FadeTo(1, (uint)(DeviceEx.AnimationsLength * 0.50), Easing.CubicIn);
		}
	}
}
