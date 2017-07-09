using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

// Borrowed from Visual Studio 2017 for Windows templates

namespace pbXNet
{
	/// <summary> 
	/// Represents a dynamic data collection that provides notifications when items get added, removed, or when the whole list is refreshed. 
	/// </summary> 
	/// <typeparam name="T"></typeparam> 
	/// <seealso cref="System.Collections.ObjectModel.ObservableCollection{T}"/>
	public class ObservableCollectionEx<T> : ObservableCollection<T>
	{
		/// <summary> 
		/// Initializes a new instance of the ObservableCollectionEx&lt;T&gt; class that contains no elements. 
		/// </summary> 
		public ObservableCollectionEx() : base()
		{
		}

		/// <summary> 
		/// Initializes a new instance of the ObservableCollectionEx&lt;T&gt; class that contains elements copied from the specified <paramref name="collection"/>. 
		/// </summary> 
		/// <exception cref="System.ArgumentNullException">The collection parameter cannot be null.</exception> 
		public ObservableCollectionEx(IEnumerable<T> collection) : base(collection)
		{
		}

		/// <summary> 
		/// Initializes a new instance of the ObservableCollectionEx&lt;T&gt; class that contains elements copied from the specified <paramref name="list"/>. 
		/// </summary> 
		/// <param name="list">The list from which the elements are copied.</param> 
		/// <exception cref="System.ArgumentNullException">The list parameter cannot be null.</exception> 
		public ObservableCollectionEx(List<T> list) : base(list)
		{
		}

		/// <summary> 
		/// Adds the elements of the specified <paramref name="collection"/> to the end of the current collection. 
		/// </summary> 
		/// <exception cref="System.ArgumentNullException">The <paramref name="collection"/> parameter cannot be null.</exception> 
		public void AddRange(IEnumerable<T> collection, NotifyCollectionChangedAction notificationMode = NotifyCollectionChangedAction.Add)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			CheckReentrancy();

			if (notificationMode == NotifyCollectionChangedAction.Reset)
			{
				foreach (var i in collection)
				{
					Items.Add(i);
				}

				OnPropertyChanged(new PropertyChangedEventArgs("Count"));
				OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

				return;
			}

			int startIndex = Count;
			var changedItems = collection is List<T> ? (List<T>)collection : new List<T>(collection);
			foreach (var i in changedItems)
			{
				Items.Add(i);
			}

			OnPropertyChanged(new PropertyChangedEventArgs("Count"));
			OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, changedItems, startIndex));
		}

		/// <summary> 
		/// Removes the first occurence of each item in the specified <paramref name="collection"/> from current collection. 
		/// </summary> 
		/// <exception cref="System.ArgumentNullException">The <paramref name="collection"/> parameter cannot be null.</exception> 
		public void RemoveRange(IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			foreach (var i in collection)
				Items.Remove(i);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		/// <summary> 
		/// Clears the current collection and replaces it with the specified <paramref name="item"/>. 
		/// </summary> 
		public void Replace(T item)
		{
			ReplaceRange(new T[] { item });
		}

		/// <summary> 
		/// Clears the current collection and replaces it with the specified <paramref name="collection"/>. 
		/// </summary> 
		/// <exception cref="System.ArgumentNullException">The <paramref name="collection"/> parameter cannot be null.</exception> 
		public void ReplaceRange(IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			Items.Clear();
			AddRange(collection, NotifyCollectionChangedAction.Reset);
		}

		/// <summary>
		/// Searches for an element that matches the conditions defined by the specified <paramref name="predicate"/>, and returns the first occurrence within the entire collection.
		/// </summary>
		/// <exception cref="System.ArgumentNullException">The <paramref name="predicate"/> parameter cannot be null.</exception> 
		public T Find(Predicate<T> predicate)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			List<T> items = Items as List<T>;
			if (items == null)
				items = new List<T>(Items);
			return items.Find(predicate);
		}

		/// <summary>
		/// Sorts the elements in the entire collection using specified <paramref name="comparison"/> or default comparer when <paramref name="comparison"/> is null.
		/// </summary>
		public void Sort(Comparison<T> comparison = null)
		{
			List<T> items = new List<T>(Items);
			if (comparison == null)
				items.Sort();
			else
				items.Sort(comparison);
			ReplaceRange(items);
		}

		/// <summary>
		/// Sorts the elements in the entire collection using specified or default <paramref name="comparer"/>.
		/// </summary>
		public void Sort(IComparer<T> comparer = null)
		{
			Sort(0, Count, comparer);
		}

		/// <summary>
		/// Sorts the elements in a range (from <paramref name="index"/> to <paramref name="index"/> + <paramref name="count"/>) 
		/// of elements in collection using specified or default <paramref name="comparer"/>.
		/// </summary>
		public void Sort(int index, int count, IComparer<T> comparer = null)
		{
			List<T> items = new List<T>(Items);
			items.Sort(index, count, comparer);
			ReplaceRange(items);
		}
	}
}