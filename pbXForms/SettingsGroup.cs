using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace pbXForms
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class SettingsGroupHeaderLayout : StackLayout { }

	[EditorBrowsable(EditorBrowsableState.Never)]
	public class SettingsGroupInnerHeaderLayout : StackLayout { }

	[EditorBrowsable(EditorBrowsableState.Never)]
	public class SettingsGroupText : Label { }

	[EditorBrowsable(EditorBrowsableState.Never)]
	public class SettingsGroupDesc : Label { }

	[EditorBrowsable(EditorBrowsableState.Never)]
	public class SettingsGroupContentLayout : StackLayout { }

	[EditorBrowsable(EditorBrowsableState.Never)]
	public class SettingsGroupImage : Image { }

	[ContentProperty("Content")]
	public class SettingsGroup : StackLayout
	{
		protected SettingsGroupHeaderLayout HeaderLayout;
		protected SettingsGroupInnerHeaderLayout InnerHeaderLayout;
		protected SettingsGroupText HeaderText;
		protected SettingsGroupDesc HeaderDesc;
		protected SettingsGroupImage HeaderImage;

		protected SettingsGroupContentLayout ContentLayout;
		public IList<View> Content => ContentLayout.Children;

		protected SettingsLineSeparator Separator;

		public string Text
		{
			get => HeaderText.Text;
			set => HeaderText.Text = value;
		}

		public string Description
		{
			get => HeaderDesc.Text;
			set {
				HeaderDesc.Text = value;
				HeaderDesc.IsVisible = !string.IsNullOrWhiteSpace(value);
			}
		}

		FileImageSource _image;
		public FileImageSource Image
		{
			get => _image;
			set {
				_image = value;
				HeaderImage.Source = _image;
			}
		}

		public bool AnimateImage { get; set; } = true;

		public bool SeparatorIsVisible
		{
			get => Separator.IsVisible;
			set => Separator.IsVisible = value;
		}

		public bool IsCollapsed
		{
			get => !ContentLayout.IsVisible;
			set {
				ContentLayout.IsVisible = !value;
				if(AnimateImage)
					HeaderImage.Rotation = value ? 0 : 90;
			}
		}

		public SettingsGroup()
		{
			Build();
		}

		protected virtual void Build()
		{
			Orientation = StackOrientation.Vertical;
			Padding = new Thickness(0);
			Margin = new Thickness(0);
			Spacing = 0;

			BuildHeader();
			BuildContent();
			BuildSeparator();

			Children.Add(HeaderLayout);
			Children.Add(ContentLayout);
			Children.Add(Separator);
		}

		protected virtual void BuildHeader()
		{
			HeaderLayout = new SettingsGroupHeaderLayout()
			{
				Orientation = StackOrientation.Horizontal,
				Spacing = Metrics.ButtonItemsSpacing,
				Padding = new Thickness(0, 0, Metrics.ButtonItemsSpacing, 0),
				Margin = new Thickness(0)
			};

			TapGestureRecognizer tgr = new TapGestureRecognizer();
			tgr.Command = new Command(OnTapped);
			HeaderLayout.GestureRecognizers.Add(tgr);

			InnerHeaderLayout = new SettingsGroupInnerHeaderLayout()
			{
				Orientation = StackOrientation.Vertical,
				HorizontalOptions = LayoutOptions.StartAndExpand,
				Spacing = Metrics.ButtonItemsSpacing / 4,
				Padding = new Thickness(Metrics.ButtonItemsSpacing, Metrics.ButtonItemsSpacing * 3, Metrics.ButtonItemsSpacing, Metrics.ButtonItemsSpacing * 2),
				Margin = new Thickness(0)
			};

			HeaderText = new SettingsGroupText()
			{
				FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
				HorizontalOptions = LayoutOptions.StartAndExpand,
			};

			HeaderDesc = new SettingsGroupDesc()
			{
				FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
				IsVisible = false,
			};

			InnerHeaderLayout.Children.Add(HeaderText);
			InnerHeaderLayout.Children.Add(HeaderDesc);

			HeaderImage = new SettingsGroupImage()
			{
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Center,
			};

			HeaderLayout.Children.Add(InnerHeaderLayout);
			HeaderLayout.Children.Add(HeaderImage);
		}

		protected virtual void BuildContent()
		{
			ContentLayout = new SettingsGroupContentLayout()
			{
				Orientation = StackOrientation.Vertical,
				Padding = new Thickness(0),
				Margin = new Thickness(0),
				Spacing = 0,
			};
		}

		protected virtual void BuildSeparator()
		{
			Separator = new SettingsLineSeparator();
		}

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

		protected virtual async void OnTapped(object parameter)
		{
			if(AnimateImage)
				HeaderImage?.RotateTo(IsCollapsed ? 90 : 0, DeviceEx.AnimationsLength);

			await InnerHeaderLayout.FadeTo(0.25, (uint)(DeviceEx.AnimationsLength * 0.50), Easing.CubicOut);

			ContentLayout.IsVisible = !ContentLayout.IsVisible;

			await InnerHeaderLayout.FadeTo(1, (uint)(DeviceEx.AnimationsLength * 0.50), Easing.CubicIn);
		}

#pragma warning restore CS4014

	}
}
