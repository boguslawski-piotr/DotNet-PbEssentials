using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace pbXNet
{
	public interface IQueryResult<T> : IEnumerable<T>
	{
		IQueryResult<T> Where(Expression<Func<T, bool>> expr);

		IQueryResult<T> OrderBy<K>(Expression<Func<T, K>> expr);

		IQueryResult<T> OrderByDescending<K>(Expression<Func<T, K>> expr);

		/// <summary>
		/// Should prepare and send a query to the database (based on data received from Where, OrderBy calls),
		/// perhaps retrieve some rows (for example using some cache strategy) and then prepare the enumerator for normal (in Linq manner) use.
		/// </summary>
		Task<IEnumerable<T>> PrepareAsync();

		Task<bool> AnyAsync();

		//CountAsync();
		//SelectAsync();
		//ToListAsync();
		//ToArrayAsync();

		// TODO: all linq extensions that can be optimized for execution in the database
	}

	public interface ITable<T>
	{
		Task CreatePrimaryKeyAsync(params Expression<Func<T, object>>[] columns);

		Task CreateIndexAsync(bool unique, params Expression<Func<T, object>>[] columns);

		IQueryResult<T> Rows { get; }

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

	public interface IDatabase
	{
		string Name { get; }

		// If table not exists then create, otherwise perform upgrade if needed.
		Task<ITable<T>> CreateTableAsync<T>(string tableName);

		Task DropTableAsync<T>(string tableName);

		//Task<T> ExecuteCommandAsync<T>(string command, params object[] args);

		//IQueryResult<T> Query<T>(string query, params object[] args);

		ITable<T> Table<T>(string tableName);
	}
}
