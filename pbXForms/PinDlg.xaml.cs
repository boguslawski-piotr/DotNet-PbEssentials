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
		public DigitBtnsArray DigitBtns;

		char[] _pin = { };
		public char[] Pin => _pin;

		Grid _grid => (View?.Children[0] as Grid);

		public PinDlg()
		{
			InitializeComponent();
			DigitBtns = new DigitBtnsArray(_grid);
		}

		public void Reset()
		{
			// Reset data from memory
			_pin?.FillWithDefault();
			_pin = new char[] { };
			UpdatePinVisualization();
		}

		public void SetCompactSize()
		{
			_grid.RowSpacing = Metrics.ButtonItemsSpacing / 4;
			_titleAndPinBar.Spacing = Metrics.ButtonItemsSpacing / 4;
			Content.WidthRequest = 240;
		}

		public void SetNormalSize()
		{
			_grid.RowSpacing = Metrics.ButtonItemsSpacing;
			_titleAndPinBar.Spacing = Metrics.ButtonItemsSpacing;
			Content.WidthRequest = 280;
		}

		public void SetLargeSize()
		{
			_grid.RowSpacing = Metrics.ButtonItemsSpacing * 2;
			_titleAndPinBar.Spacing = Metrics.ButtonItemsSpacing * 2;
			Content.WidthRequest = 320;
		}

		//

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

		public class DigitBtnsArray
		{
			readonly Grid _grid;

			public DigitBtnsArray(Grid grid)
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
