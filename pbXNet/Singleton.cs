using System;
using System.Collections.Concurrent;

namespace pbXNet
{
	/// <summary>
	/// Singleton.
	/// </summary>
	internal static class Singleton<T> where T : new()
	{
		static readonly ConcurrentDictionary<Type, T> _instances = new ConcurrentDictionary<Type, T>();

		public static T Instance
		{
			get {
				return _instances.GetOrAdd(typeof(T), (t) => new T());
			}
		}
	}

}
