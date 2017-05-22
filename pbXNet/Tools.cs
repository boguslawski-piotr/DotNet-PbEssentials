using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace pbXNet
{
    /// <summary>
    /// Settings.
    /// </summary>
    static class Settings
    {
        internal static readonly JsonSerializerSettings JsonSerializer = new JsonSerializerSettings()
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore,
#if DEBUG
            Formatting = Formatting.Indented,
#endif
        };
	}
	

    /// <summary>
	/// Tools.
	/// </summary>
	public static class Tools
    {
        public static bool IsDifferent<T>(T a, ref T b)
        {
            if (Equals(a, b))
                return false;
            b = a;
            return true;
        }

        public static string CreateGuid()
        {
            return System.Guid.NewGuid().ToString("N");
        }
    }


	/// <summary>
    /// ObservableAsync.
    /// </summary>
    public class Observable : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

        protected void Set<T>(ref T storage, T value, [CallerMemberName]string name = null)
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


    /// <summary>
    /// Singleton.
    /// </summary>
	internal static class Singleton<T> where T : new()
	{
        static readonly ConcurrentDictionary<Type, T> _instances = new ConcurrentDictionary<Type, T>();

        public static T Instance
		{
			get
			{
				return _instances.GetOrAdd(typeof(T), (t) => new T());
			}
		}
	}

}
