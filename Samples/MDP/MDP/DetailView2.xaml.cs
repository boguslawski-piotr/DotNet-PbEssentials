using pbXForms;
using Xamarin.Forms;

namespace MDP
{
	public partial class DetailView2 : pbXForms.ContentView
	{
		public DetailView2 ()
		{
			InitializeComponent ();
		}

		MastersDetailsPage mp => Application.Current.MainPage as MastersDetailsPage;

		public void master1(object sender, System.EventArgs e)
		{
			mp.ShowMasterViewAsync<MasterView1>(MastersDetailsPage.ViewsSwitchingAnimation.Back);
		}

		public void master2(object sender, System.EventArgs e)
		{
			mp.ShowMasterViewAsync<MasterView2>(MastersDetailsPage.ViewsSwitchingAnimation.Back);
		}

		public void detail1(object sender, System.EventArgs e)
		{
			mp.ShowDetailViewAsync<DetailView1>(MastersDetailsPage.ViewsSwitchingAnimation.Back);
		}
	}
}