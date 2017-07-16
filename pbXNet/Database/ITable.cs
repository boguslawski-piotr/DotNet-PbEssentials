using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PrimaryKeyAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Property)]
	public class IndexAttribute : Attribute
	{
		public IndexAttribute(string name, bool desc = false)
		{ }
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class NotNullAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Property)]
	public class LengthAttribute : Attribute
	{
		public LengthAttribute(int width)
		{ }
	}

	public interface ITable<T>: IDisposable
	{
		IQueryEx<T> Rows { get; }

		// Returns the element that matches primary key, if found; otherwise, the default value for type T.
		// If table don't have primary key then it should throw an exception.
		Task<T> FindAsync(T pk);

		// If primary key is defined then:
		// if the row with primary key exists should update, otherwise insert new.
		// If table don't have primary key then:
		// insert new.
		Task InsertAsync(T o);

		// If the element with primary key not exists should throw an exception.
		Task UpdateAsync(T o);

		// If primary key is defined then:
		// if the row with primary key exists should delete it, otherwise do nothing.
		// If table don't have primary key then:
		// should delete all rows that matching o.
		Task DeleteAsync(T o);
	}
}
