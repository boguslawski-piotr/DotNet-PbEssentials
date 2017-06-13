﻿using System;
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
	public class ObservableCollectionEx<T> : ObservableCollection<T>
	{
		/// <summary> 
		/// Initializes a new instance of the System.Collections.ObjectModel.ObservableCollection(Of T) class. 
		/// </summary> 
		public ObservableCollectionEx() : base()
		{
		}

		/// <summary> 
		/// Initializes a new instance of the System.Collections.ObjectModel.ObservableCollection(Of T) class that contains elements copied from the specified collection. 
		/// </summary> 
		/// <param name="collection">The collection from which the elements are copied.</param> 
		/// <exception cref="System.ArgumentNullException">The collection parameter cannot be null.</exception> 
		public ObservableCollectionEx(IEnumerable<T> collection) : base(collection)
		{
		}

		/// <summary> 
		/// Adds the elements of the specified collection to the end of the ObservableCollection(Of T). 
		/// </summary> 
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
		/// Removes the first occurence of each item in the specified collection from ObservableCollection(Of T). 
		/// </summary> 
		public void RemoveRange(IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			foreach (var i in collection)
				Items.Remove(i);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		/// <summary> 
		/// Clears the current collection and replaces it with the specified item. 
		/// </summary> 
		public void Replace(T item)
		{
			ReplaceRange(new T[] { item });
		}

		/// <summary> 
		/// Clears the current collection and replaces it with the specified collection. 
		/// </summary> 
		public void ReplaceRange(IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			Items.Clear();
			AddRange(collection, NotifyCollectionChangedAction.Reset);
		}

		/// <summary>
		/// Searches for an element that matches the conditions defined by the specified predicate, and returns the first occurrence within the entire collection.
		/// </summary>
		/// <param name="match">The Predicate(Of T) delegate that defines the conditions of the element to search for.</param>
		/// See <see cref="System.Collections.Generic.List"/> for more information.
		/// <seealso cref="System.Predicate"/>
		public T Find(Predicate<T> match)
		{
			List<T> items = Items as List<T>;
			if (items == null)
				items = new List<T>(Items);
			return items.Find(match);
		}

		/// <summary>
		/// Sorts the elements in the entire collection.
		/// </summary>
		/// <param name="comparison">The System.Comparison(Of T) to use when comparing elements, or null to use the default comparer.</param>
		/// See <see cref="System.Collections.Generic.List"/> for more information.
		/// <seealso cref="System.Comparison"/>
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
		/// Sorts the elements in the entire collection.
		/// </summary>
		/// <param name="comparer">The IComparer(Of T) implementation to use when comparing elements, or null to use the default comparer Comparer(Of T).Default.</param>
		/// See <see cref="System.Collections.Generic.List"/> for more information.
		/// <seealso cref="System.Collections.Generic.IComparer"/>
		public void Sort(IComparer<T> comparer = null)
		{
			Sort(0, Count, comparer);
		}

		/// <summary>
		/// Sorts the elements in a range of elements in collection.
		/// </summary>
		/// <param name="index">The zero-based starting index of the range to sort.</param>
		/// <param name="count">The length of the range to sort.</param>
		/// <param name="comparer">The IComparer(Of T) implementation to use when comparing elements, or null to use the default comparer Comparer(Of T).Default.</param>
		/// See <see cref="System.Collections.Generic.List"/> for more information.
		/// <seealso cref="System.Collections.Generic.IComparer"/>
		public void Sort(int index, int count, IComparer<T> comparer = null)
		{
			List<T> items = new List<T>(Items);
			items.Sort(index, count, comparer);
			ReplaceRange(items);
		}
	}
}