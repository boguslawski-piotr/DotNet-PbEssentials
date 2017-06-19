using pbXForms;

namespace MDP
{
	public partial class NavDrawer : ModalContentView
	{
		public NavDrawer ()
		{
			InitializeComponent ();
		}

		public void close(object sender, System.EventArgs e)
		{
			OnOK();
		}
	}
}