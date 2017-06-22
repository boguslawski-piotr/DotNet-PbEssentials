using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace pbXNet
{
	/// <summary>
	/// A base class that allows you to observe changes in properties.
	/// </summary>
	/// <example>
	/// <code>
	/// class Window : Observable {
	///   ...
	///   bool _open;
	///   public bool Open {
	///     get => _open;
	///     set => SetValue(ref _open, value);
	///   }
	///   ...
	/// }
	/// 
	/// class HomeControl {
	///   ...
	///   bool _armed;
	///   List<Window> _windows = new List<Window>();
	///   ...
	///   public AddWindow(Window window) {
	///     _windows.Add(window);
	///     window.PropertyChanged += OnWindowPropertyChanged;
	///   }
	///   public OnWindowPropertyChanged(object sender, PropertyChangedEventArgs a) {
	///     if(a.PropertyName == "Open")
	///       if((sender as Window).Open && _armed)
	///         FireAlarm(...);
	///   }
	///   ...
	/// }
	/// </code>
	/// </example>
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
