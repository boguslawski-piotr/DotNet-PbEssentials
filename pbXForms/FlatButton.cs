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
		public FlatButton()
		{
			HeightRequest = Metrics.TouchTargetHeight;
			MinimumWidthRequest = Metrics.TouchTargetHeight;
			VerticalOptions = LayoutOptions.Center;
#if !__IOS__
			VerticalTextAlignment = TextAlignment.Center;
#endif
		}
	}
}
