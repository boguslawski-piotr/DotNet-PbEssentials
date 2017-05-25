using System;
using System.Windows.Input;
using Xamarin.Forms;

namespace pbXForms
{
    public class ImageEx : Image
    {
		public static readonly BindableProperty CommandProperty = BindableProperty.Create("Command", typeof(ICommand), typeof(ImageEx), null,
			propertyChanged: (bo, o, n) => ((ImageEx)bo).OnCommandChanged());

		public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create("CommandParameter", typeof(object), typeof(ImageEx), null,
			propertyChanged: (bo, o, n) => ((ImageEx)bo).CommandCanExecuteChanged(bo, EventArgs.Empty));

		public ICommand Command
		{
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}

		public object CommandParameter
		{
			get { return GetValue(CommandParameterProperty); }
			set { SetValue(CommandParameterProperty, value); }
		}
		
        public event EventHandler Clicked;

        public ImageEx()
        {
			TapGestureRecognizer tgr = new TapGestureRecognizer();
			tgr.Command = new Command(OnTapped);
			this.GestureRecognizers.Add(tgr);
		}

		protected override void OnPropertyChanging(string propertyName = null)
		{
			if (propertyName == CommandProperty.PropertyName)
			{
				ICommand cmd = Command;
				if (cmd != null)
					cmd.CanExecuteChanged -= CommandCanExecuteChanged;
			}

			base.OnPropertyChanging(propertyName);
		}

		void CommandCanExecuteChanged(object sender, EventArgs eventArgs)
		{
			ICommand cmd = Command;
			if (cmd != null)
			{
				IsEnabled = cmd.CanExecute(CommandParameter);
			}
		}

		void OnCommandChanged()
		{
			if (Command != null)
			{
				Command.CanExecuteChanged += CommandCanExecuteChanged;
				CommandCanExecuteChanged(this, EventArgs.Empty);
			}
			else
			{
				IsEnabled = true;
			}
		}

		async void OnTapped(object parameter)
		{
            if (!IsEnabled)
                return;
            
			double opacity = Opacity;

			await this.FadeTo(0.2, 150);

			Command?.Execute(CommandParameter);
			Clicked?.Invoke(this, EventArgs.Empty);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			this.FadeTo(opacity, 150);
#pragma warning restore CS4014
		}
	}
}
