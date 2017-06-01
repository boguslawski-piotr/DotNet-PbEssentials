using System;
using System.Threading;
using System.Threading.Tasks;
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

		volatile Int32 _onTappedIsRunning = 0;
		
        void OnTapped(object parameter)
		{
            if (!IsEnabled || Interlocked.Exchange(ref _onTappedIsRunning, 1) == 1)
                return;
            
			StartTappedAnimation();

			Command?.Execute(CommandParameter);
			Clicked?.Invoke(this, EventArgs.Empty);
			
            Interlocked.Exchange(ref _onTappedIsRunning, 0);
		}

		protected virtual void StartTappedAnimation()
		{
			Task.Run(async () =>
			{
				await this.ScaleTo(1.25, (uint)(DeviceEx.AnimationsLength * 0.65), Easing.CubicOut);
				await this.ScaleTo(1, (uint)(DeviceEx.AnimationsLength * 0.35), Easing.CubicIn);
			});

		}
	}
}
