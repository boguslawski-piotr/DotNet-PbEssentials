using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using pbXNet;

namespace pbXForms
{
    //[XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ContentPageEx : ContentPage
    {
        protected ContentViewEx _Content => (ContentViewEx)Content;

        public AppBarLayout AppBar => _Content.AppBar;
        public IList<View> AppBarContent => _Content.AppBarContent;

        public IList<View> PageContent => _Content.ViewContent;

        public ToolBarLayout ToolBar => _Content.ToolBar;
        public IList<View> ToolBarContent => _Content.ToolBarContent;

        public ContentPageEx()
        {
            InitializeComponent();
        }
    }
}
