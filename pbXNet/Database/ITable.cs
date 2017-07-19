using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public interface ITable<T> : IDisposable where T : new()
	{
		string Name { get; }

		Task CreateAsync();

		Task OpenAsync();

		// Each call should return a new object.
		IQuery<T> Rows { get; }

		// Returns true if the element that matches primary key exists.
		Task<bool> ExistsAsync(T pk);

		// Returns the element that matches primary key, if found; otherwise, the default value for type T.
		// If table don't have primary key then it should throw an exception.
		Task<T> FindAsync(T pk);

		// If the element with primary key not exists should throw an exception.
		Task UpdateAsync(T o);

		Task<int> UpdateAsync<TA>(TA o, Expression<Func<TA, bool>> predicate);

		// If primary key is defined then:
		// if the row with primary key exists should update, otherwise insert new.
		// If table don't have primary key then:
		// insert new.
		Task InsertOrUpdateAsync(T o);

		// If primary key is defined then:
		// if the row with primary key exists should delete it, otherwise do nothing.
		// If table don't have primary key then:
		// should delete all rows that exactly matching o.
		Task<int> DeleteAsync(T o);

		// If predicate cannot be compiled/transalted/etc. should throw an exception.
		Task<int> DeleteAsync(Expression<Func<T, bool>> predicate);
	}
}
