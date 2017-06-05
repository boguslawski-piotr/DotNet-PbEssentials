using System;
using Xamarin.Forms;

namespace pbXForms
{
	public class ModalContentView : ContentView
	{
		public event EventHandler OK;
		public event EventHandler Cancel;

		public ModalContentView()
		{
		}

		public virtual void OnOK()
		{
			OK?.Invoke(this, null);
		}

		public virtual void OnCancel()
		{
			Cancel?.Invoke(this, null);
		}
	}
}
