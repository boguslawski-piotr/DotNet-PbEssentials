using System.Collections.Generic;
using Xamarin.Forms;

namespace pbXForms
{
	public partial class ContentPage : Xamarin.Forms.ContentPage
	{
		public new ContentView Content => (pbXForms.ContentView)base.Content;

		public IList<View> AppBarContent => Content.AppBarContent;
		public IList<View> ViewContent => Content.ViewContent;
		public IList<View> ToolBarContent => Content.ToolBarContent;

		public ModalViewsManager ModalManager => Content.ModalManager;

		public ContentPage()
		{
			InitializeComponent();
		}
	}
}
