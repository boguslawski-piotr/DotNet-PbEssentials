using System.Collections.Generic;
using Xamarin.Forms;

namespace pbXForms
{
    public partial class ContentPageEx : ContentPage
    {
        public AppBarLayout AppBar => _content.AppBar;
        public IList<View> AppBarContent => _content.AppBarContent;

        public IList<View> PageContent => _content.ViewContent;

        public ToolBarLayout ToolBar => _content.ToolBar;
        public IList<View> ToolBarContent => _content.ToolBarContent;

        public ModalViewsManager ModalManager => _content.ModalManager;

        ContentViewEx _content => (ContentViewEx)Content;

		public ContentPageEx()
        {
            InitializeComponent();
        }
    }
}
