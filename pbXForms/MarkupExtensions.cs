using System;
using pbXNet;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace pbXForms
{
	[ContentProperty("Text")]
	public class LocalizedExtension : IMarkupExtension
	{
		public string Text { get; set; }

		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return Localized.T(Text);
		}
	}
}
