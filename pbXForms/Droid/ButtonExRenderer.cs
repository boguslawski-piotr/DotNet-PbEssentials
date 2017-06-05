#if __ANDROID__

using CustomRenderer.Android;
using pbXForms;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(ButtonEx), typeof(ButtonExRenderer))]
namespace CustomRenderer.Android
{
	class ButtonExRenderer : ButtonRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
		{
			base.OnElementChanged(e);

			// TODO: make it similar (in look and behavior) to iOS button

			if (Element.BackgroundColor == Xamarin.Forms.Color.Default)
				Control.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
		}
	}
}

#endif
