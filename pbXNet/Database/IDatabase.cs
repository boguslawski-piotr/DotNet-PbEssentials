using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public interface IDatabase
	{
		string Name { get; }

		SqlBuilder SqlBuilder { get; }

		//

		Task OpenAsync();
		Task CloseAsync();

		//

		Task<int> StatementAsync(string sql, params (string name, object value)[] parameters);
		Task<int> StatementAsync(string sql, params object[] parameters);
		Task<int> StatementAsync(string sql);

		Task<T> ScalarAsync<T>(string sql, params (string name, object value)[] parameters);
		Task<T> ScalarAsync<T>(string sql, params object[] parameters);
		Task<T> ScalarAsync<T>(string sql);

		Task<IQuery<T>> QueryAsync<T>(string sql, params (string name, object value)[] parameters);
		Task<IQuery<T>> QueryAsync<T>(string sql, params object[] parameters);
		Task<IQuery<T>> QueryAsync<T>(string sql);

		//

		Task<bool> TableExistsAsync(string tableName);

		// If table not exists then create, otherwise perform upgrade if needed.
		Task<ITable<T>> CreateTableAsync<T>(string tableName);

		Task DropTableAsync(string tableName);

		ITable<T> Table<T>(string tableName);
		Task<ITable<T>> TableAsync<T>(string tableName);
	}
}
