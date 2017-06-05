using System;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
	public partial class PinDlg : ContentViewEx
	{
		public Label Title => _title;

		public char Dot { get; set; } = '•';
		public Label PinVisualization => _pinVisualization;

		public PIButton DelBtn => _delBtn;
		public PIButton OKBtn => _okBtn;
		public BtnBridge Btn;

		char[] _pin = { };
		public char[] Pin => _pin;

		public PinDlg()
		{
			InitializeComponent();
			Btn = new BtnBridge((View.Children[0] as Grid));
		}

		public void Reset()
		{
			// Reset data from memory
			_pin?.FillWithDefault();
			_pin = new char[] { };
			UpdatePinVisualization();
		}

		void UpdatePinVisualization()
		{
			_pinVisualization.Text = _pin.Length > 0 ? "".PadRight(_pin.Length, Dot) : " ";
		}

		void Digit_Clicked(object sender, System.EventArgs e)
		{
			if (sender is PIButton btn)
			{
				Array.Resize<char>(ref _pin, _pin.Length + 1);
				_pin[_pin.Length - 1] = ((string)btn.CommandParameter)[0];
				UpdatePinVisualization();
			}
		}

		void Del_Clicked(object sender, System.EventArgs e)
		{
			if (_pin.Length > 0)
			{
				Array.Resize<char>(ref _pin, _pin.Length - 1);
				UpdatePinVisualization();
			}
		}


		//

		public class BtnBridge
		{
			Grid _grid;

			public BtnBridge(Grid grid)
			{
				_grid = grid;
			}

			public PIButton this[int index]
			{
				get
				{
					if (_grid != null && index >= 0 && index <= 9)
					{
						foreach (var c in _grid.Children)
							if (c is PIButton b)
							{
								if (b.CommandParameter != null && b.CommandParameter.ToString() == index.ToString())
									return b;
							}
					}
					return null;
				}
			}
		}
	}
}
