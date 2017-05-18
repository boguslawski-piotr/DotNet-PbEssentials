﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace pbXNet
{
    public static class Tools
    {
        public static bool IsDifferent<T>(T a, ref T b)
        {
            if (Equals(a, b))
                return false;
            b = a;
            return true;
        }
    }

	public class Observable : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
		{
			if (Equals(storage, value))
			{
				return;
			}

			storage = value;
			OnPropertyChanged(propertyName);
		}

		virtual protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	internal static class Singleton<T> where T : new()
	{
		private static ConcurrentDictionary<Type, T> _instances = new ConcurrentDictionary<Type, T>();

		public static T Instance
		{
			get
			{
				return _instances.GetOrAdd(typeof(T), (t) => new T());
			}
		}
	}

}
