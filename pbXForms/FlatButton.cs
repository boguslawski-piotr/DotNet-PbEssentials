using System;
using Xamarin.Forms;

namespace pbXForms
{
#if __IOS__
	public class FlatButton : Button
#else
	public class FlatButton : Label
#endif
	{
#if !__IOS__
		public event EventHandler Clicked;
#endif
		public FlatButton()
		{
			HeightRequest = Metrics.TouchTargetHeight;
			MinimumWidthRequest = Metrics.TouchTargetHeight;
			VerticalOptions = LayoutOptions.Center;
#if !__IOS__
			VerticalTextAlignment = TextAlignment.Center;

			TapGestureRecognizer tgr = new TapGestureRecognizer()
			{
				Command = new Command(OnTapped)
			};
			this.GestureRecognizers.Add(tgr);
#endif
		}

#if !__IOS__
		void OnTapped(object parameter)
		{
			Clicked?.Invoke(this, null);
		}
#endif
	}
}
