using System.ComponentModel;
using System.Runtime.CompilerServices;

#if PLUGIN_PBXSETTINGS
namespace Plugin.pbXSettings.pbXNet
#else
namespace pbXNet
#endif

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
	///   List&lt;Window&gt; _windows = new List&lt;Window&gt;();
	///   ...
	///   public AddWindow(Window window) {
	///     _windows.Add(window);
	///     window.PropertyChanged += OnWindowPropertyChanged;
	///   }
	///   public OnWindowPropertyChanged(object sender, PropertyChangedEventArgs a) {
	///     if(!_armed)
	///       return;
	///     if(a.PropertyName == "Open")
	///       if((sender as Window).Open)
	///         FireAlarm(...);
	///   }
	///   ...
	/// }
	/// </code>
	/// </example>
	/// <seealso cref="INotifyPropertyChanged"/>
	public class Observable : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected bool SetValue<T>(ref T storage, T value, [CallerMemberName]string name = null)
		{
			if (Equals(storage, value))
			{
				return false;
			}

			storage = value;
			OnPropertyChanged(name);

			return true;
		}

		protected virtual void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}

}
