#if __MACOS__

using CustomRenderer.MacOS;
using pbXForms;
using Xamarin.Forms;
using Xamarin.Forms.Platform.MacOS;

[assembly: ExportRenderer(typeof(ButtonEx), typeof(ButtonExRenderer))]
namespace CustomRenderer.MacOS
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
