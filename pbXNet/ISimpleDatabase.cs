using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace pbXNet
{
	public interface IQueryResult<T> : IEnumerable<T>
	{
		IQueryResult<T> Where(Expression<Func<T, bool>> expr);

		// group by ???

		IQueryResult<T> OrderBy<K>(Expression<Func<T, K>> expr);

		IQueryResult<T> OrderByDescending<K>(Expression<Func<T, K>> expr);

		/// <summary>
		/// Should prepare and send a query to the database (based on data received from Where, GroupBy, OrderBy calls),
		/// retrieve some rows (for example using some cache strategy) and then prepare the enumerator.
		/// </summary>
		Task<IEnumerable<T>> PrepareAsync();

		Task<bool> AnyAsync();

		//FirstAsync();
		//FirstOrDefaultAsync();
		//CountAsync();
		//ToListAsync();
		//etc...
	}

	public interface ITable<T>
	{
		Task CreatePrimaryKeyAsync(params string[] columnNames);

		Task CreateIndexAsync(bool unique, params string[] columnNames);

		IQueryResult<T> Rows { get; }

		// Only properties that are part of the primary key are used.
		// Returns the element that matches primary key, if found; otherwise, the default value for type T.
		Task<T> FindAsync(T pk);

		// If the element with primary key exists should update, otherwise insert new.
		Task InsertAsync(T o);

		// If the element with primary key not exists should throw an exception.
		Task UpdateAsync(T o);

		// Only properties that are part of the primary key are used.
		// If the element with primary key not exists should throw an exception.
		Task DeleteAsync(T pk);
	}

	public interface ISimpleDatabase
	{
		string Name { get; }

		// Can be called multiple times!
		Task InitializeAsync();

		// If table not exists then create, otherwise perform upgrade if needed.
		Task<ITable<T>> CreateTableAsync<T>(string tableName);

		Task DropTableAsync<T>(string tableName);

		//int Execute<T>(string query, params object[] args);

		//IQueryResult<T> Query<T>(string query, params object[] args);

		ITable<T> Table<T>(string tableName);
	}
}
