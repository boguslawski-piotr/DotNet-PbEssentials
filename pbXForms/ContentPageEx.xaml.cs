using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using pbXNet;

namespace pbXForms
{
    public partial class ContentPageEx : ContentPage
    {
        protected ContentViewEx _Content => (ContentViewEx)Content;

        public AppBarLayout AppBar => _Content.AppBar;
        public IList<View> AppBarContent => _Content.AppBarContent;
        public Color AppBarBackgroundColor
        {
            get { return _Content.AppBarBackgroundColor; }
            set { _Content.AppBarBackgroundColor = value; }
        }

        public IList<View> PageContent => _Content.ViewContent;

        public ToolBarLayout ToolBar => _Content.ToolBar;
        public IList<View> ToolBarContent => _Content.ToolBarContent;
        public Color ToolBarBackgroundColor
        {
            get { return _Content.ToolBarBackgroundColor; }
            set { _Content.ToolBarBackgroundColor = value; }
        }

        public ModalViewsManager ModalManager => _Content.ModalManager;

        public ContentPageEx()
        {
            InitializeComponent();
        }
    }
}
