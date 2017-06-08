using System;
using Xamarin.Forms;

namespace pbXForms
{
	public class ModalContentView : ContentView
	{
		// TODO: all make bindable

		/// Gets or sets a value indicating whether this has shadow while displayed as a modal.
		public bool HasShadow { get; set; } = false;

		/// Gets or sets the frame corner radius.
		public float CornerRadius { get; set; } = 6;

		public static readonly BindableProperty MaximumWidthRequestProperty = BindableProperty.Create("MaximumWidthRequest", typeof(double), typeof(ModalContentView), -1d, propertyChanged: OnRequestChanged);
		public static readonly BindableProperty MaximumHeightRequestProperty = BindableProperty.Create("MaximumHeightRequest", typeof(double), typeof(ModalContentView), -1d, propertyChanged: OnRequestChanged);

		public double MaximumHeightRequest
		{
			get => (double)GetValue(MaximumHeightRequestProperty);
			set => SetValue(MaximumHeightRequestProperty, value);
		}

		public double MaximumWidthRequest
		{
			get => (double)GetValue(MaximumWidthRequestProperty);
			set => SetValue(MaximumWidthRequestProperty, value);
		}

		public double RelativeWidthWhenNavDrawer { get; set; } = 0.8d;
		public double WidthInPortraitWhenNavDrawer { get; set; } = -1d;
		public double WidthInLandscapeWhenNavDrawer { get; set; } = -1d;


		//

		/// How and where this view was displayed by the modal views manager.
		/// It should be readonly/private set but C# does not have the mechanism of friendly classes like C++ does :(
		public ModalViewsManager.ModalPosition Position = ModalViewsManager.ModalPosition.WholeView;

		/// Gets a value indicating whether this view covers status bar.
		public bool ViewCoversStatusBar => (Position == ModalViewsManager.ModalPosition.WholeView) && Device.RuntimePlatform == Device.iOS && DeviceEx.StatusBarVisible;


		//

		public event EventHandler OK;
		public event EventHandler Cancel;

		/// Indicates that the view is about to appearwhen displayed as modal.
		public event EventHandler AppearingWhenModal;

		/// Indicates that the view is about to cease displaying when was displayed as modal.
		public event EventHandler DisappearingWhenModal;


		//

		/// Allows application developers to customize behavior immediately prior to the view becoming visible when displayed as modal.
		public virtual void OnAppearingWhenModal()
		{
			AppearingWhenModal?.Invoke(this, null);
		}

		/// Allows the application developer to customize behavior as the view disappears when was displayed as modal.
		public virtual void OnDisappearingWhenModal()
		{
			DisappearingWhenModal?.Invoke(this, null);
		}

		public void OK_Clicked(object sender, System.EventArgs e)
		{
			OnOK();
		}

		public virtual void OnOK()
		{
			OK?.Invoke(this, null);
		}

		public void Cancel_Clicked(object sender, System.EventArgs e)
		{
			OnCancel();
		}

		public virtual void OnCancel()
		{
			Cancel?.Invoke(this, null);
		}

		//

		static void OnRequestChanged(BindableObject o, object ov, object nv)
		{
			// TODO: odpowiednio reagowac?
			// chyba trzeba wywolac AnimateModalAsync z ModalViewsManager
			// i wtedy tez trzeba jakos przechwycic takze zmiany w MinimumWidthRequest/MinimumHeightRequest
		}

	}
}
