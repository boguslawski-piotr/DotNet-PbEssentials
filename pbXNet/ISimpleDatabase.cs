using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace pbXNet
{
	public interface ISimpleDatabase
	{
		string Name { get; }

		// Can be called multiple times!
		Task InitializeAsync();

		// If table not exists then create, otherwise perform upgrade if needed.
		Task CreateTableAsync<T>(string tableName);

		Task CreatePrimaryKeyAsync<T>(string tableName, params string[] columnNames);

		Task CreateIndexAsync<T>(string tableName, bool unique, params string[] columnNames);

		// If nothing found should return Enumerable.Empty<T>()
		Task<IEnumerable<T>> SelectAsync<T>(string tableName, Expression<Func<T, bool>> where);

		// Only properties that are part of the primary key are used.
		// Returns the element that matches primary key, if found; otherwise, the default value for type T.
		Task<T> FindAsync<T>(string tableName, T pk);

		// If the element with primary key exists should update, otherwise insert new.
		Task InsertAsync<T>(string tableName, T o);

		// If the element with primary key not exists should throw an exception.
		Task UpdateAsync<T>(string tableName, T o);

		// Only properties that are part of the primary key are used.
		// If the element with primary key not exists should throw an exception.
		Task DeleteAsync<T>(string tableName, T pk);
	}
}
