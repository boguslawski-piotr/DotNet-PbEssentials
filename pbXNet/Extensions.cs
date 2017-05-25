using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace pbXNet
{
	/// <summary>
    /// String extensions.
    /// </summary>
    public static class StringExtensions
	{
		public static byte[] ToByteArray(this string src)
		{
			return Encoding.UTF8.GetBytes(src);
		}

		public static byte[] FromHexString(this string src)
		{
			return ConvertEx.FromHexString(src);
		}

		public static MemoryStream ToMemoryStream(this string src)
		{
			return ConvertEx.ToMemoryStream(src);
		}
	}

	/// <summary>
    /// Collection extensions.
    /// </summary>
    public static class CollectionExtensions
	{
		//public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
		//{
		//    var sortableList = new List<T>(collection);
		//    if (comparison == null)
		//        sortableList.Sort();
		//    else
		//        sortableList.Sort(comparison);

		//    for (var i = 0; i < sortableList.Count; i++)
		//    {
		//        var oldIndex = collection.IndexOf(sortableList[i]);
		//        var newIndex = i;
		//        if (oldIndex != newIndex)
		//            collection.Move(oldIndex, newIndex);
		//    }
		//}

		// or

		public static void Sort<T, V>(this ObservableCollection<T> observableCollection, Func<T, V> keySelector)
		{
			var l = observableCollection.OrderBy(keySelector).ToList();
			observableCollection.Clear();
			foreach (var v in l)
			{
				observableCollection.Add(v);
			}
		}
	}

}
