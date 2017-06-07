using System;
using Xamarin.Forms;

namespace pbXForms
{
	public class ModalContentView : ContentView
	{
		public event EventHandler OK;
		public event EventHandler Cancel;

		public ModalViewsManager.ModalPosition Position = ModalViewsManager.ModalPosition.WholeView;

		public bool ViewCoversStatusBar => (Position == ModalViewsManager.ModalPosition.WholeView) && Device.RuntimePlatform == Device.iOS;

		public ModalContentView()
		{
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
	}
}
