#if WINDOWS_UWP

using Xamarin.Forms;

[assembly: ExportRenderer(typeof(ButtonEx), typeof(ButtonExRenderer))]
namespace CustomRenderer.UWP
{
	class ButtonExRenderer : ButtonRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
		{
			base.OnElementChanged(e);

			// TODO: make it similar (in look and behavior) to iOS button

		}
	}
}

#endif
