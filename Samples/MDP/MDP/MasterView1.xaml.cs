using pbXForms;
using Xamarin.Forms;

namespace MDP
{
	public partial class MasterView1 : pbXForms.ContentView
	{
		public MasterView1 ()
		{
			InitializeComponent ();
		}

		MastersDetailsPage mp => Application.Current.MainPage as MastersDetailsPage;

		public void master2(object sender, System.EventArgs e)
		{
			mp.ShowMasterViewAsync<MasterView2>(MastersDetailsPage.ViewsSwitchingAnimation.Forward);
		}

		public void detail1(object sender, System.EventArgs e)
		{
			mp.ShowDetailViewAsync<DetailView1>(MastersDetailsPage.ViewsSwitchingAnimation.Forward);
		}

		public void detail2(object sender, System.EventArgs e)
		{
			mp.ShowDetailViewAsync<DetailView2>(MastersDetailsPage.ViewsSwitchingAnimation.Forward);
		}

		public async void navdrawer(object sender, System.EventArgs e)
		{
			ModalContentView navDrawer = new NavDrawer();

			if (mp.IsSplitView)
				navDrawer.WidthInLandscapeWhenNavDrawer = mp.MasterViewWidthInSplitView;

			await mp.ModalManager.DisplayModalAsync(navDrawer, ModalViewsManager.ModalPosition.NavDrawer);
		}
	}
}