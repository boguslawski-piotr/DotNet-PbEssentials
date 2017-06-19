using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace pbXNet
{
	/// <summary>
	/// TODO: summary
	/// </summary>
	/// <seealso cref="System.ComponentModel.INotifyPropertyChanged"/>
	public class Observable : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void SetValue<T>(ref T storage, T value, [CallerMemberName]string name = null)
		{
			if (Equals(storage, value))
			{
				return;
			}

			storage = value;
			OnPropertyChanged(name);
		}

		protected virtual void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}

}
