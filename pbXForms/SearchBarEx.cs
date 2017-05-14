using System;
using Xamarin.Forms;

namespace pbXForms
{
	public class SearchBarEx : Xamarin.Forms.SearchBar
	{
		public SearchBarEx()
		{
			//Margin = new Thickness(0);
		}

		public void Show()
		{
			IsVisible = true;
			Focus();
		}

		public void Hide()
		{
			IsVisible = false;
			// TODO: hide keyboard (how?)
		}

		public void ChangeVisiblity()
		{
			if (IsVisible)
				Hide();
			else
				Show();
		}

	}
}
