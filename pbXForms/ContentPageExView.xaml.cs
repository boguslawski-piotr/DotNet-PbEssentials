using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace pbXForms
{
    public partial class ContentPageExView : ContentView
    {
        public Grid _View => __View;

        public Layout<View> AppBarRow => _AppBarRow;
        public IList<View> AppBarContent => _AppBarRow.Children;

        public Layout<View> ContentRow => _ContentRow;
        public IList<View> ContentEx => _ContentRow.Children;

        public Layout<View> ToolBarRow => _ToolBarRow;
        public IList<View> ToolBarContent => _ToolBarRow.Children;

        public ContentPageExView()
        {
            InitializeComponent();
        }
    }
}
