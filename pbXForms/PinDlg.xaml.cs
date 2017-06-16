using System;
using pbXNet;
using Xamarin.Forms;

namespace pbXForms
{
	public partial class PinDlg : ModalContentView
	{
		public Label Title => _title;

		public char Dot { get; set; } = '•';
		public Label PinVisualization => _pinVisualization;

		public PIButton DelBtn => _delBtn;
		public PIButton OKBtn => _okBtn;
		public DigitBtnsArray DigitBtns;

		Password _pin = new Password();
		public Password Pin => _pin;

		Grid _grid => (Content as Grid);

		public PinDlg()
		{
			InitializeComponent();
			DigitBtns = new DigitBtnsArray(_grid);
		}

		public void Reset()
		{
			_pin.Dispose();
			UpdatePinVisualization();
		}

		public void SetCompactSize()
		{
			_grid.RowSpacing = Metrics.ButtonItemsSpacing / 2;
			_grid.Padding = new Thickness(0);
			_titleAndPinBar.Spacing = Metrics.ButtonItemsSpacing / 2;
		}

		public void SetNormalSize()
		{
			_grid.RowSpacing = Metrics.ButtonItemsSpacing;
			_titleAndPinBar.Spacing = Metrics.ButtonItemsSpacing;
			Content.WidthRequest = 280;
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
				_pin.Append(((string)btn.CommandParameter)[0]);
				UpdatePinVisualization();
			}
		}

		void Del_Clicked(object sender, System.EventArgs e)
		{
			if (_pin.Length > 0)
			{
				_pin.RemoveLast();
				UpdatePinVisualization();
			}
		}

		public override void OnOK()
		{
			if (_pin.Length <= 0)
				OnCancel();
			else
				base.OnOK();
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
