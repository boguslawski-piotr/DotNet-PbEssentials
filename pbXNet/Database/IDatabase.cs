using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public interface IDatabase : IDisposable
	{
		string Name { get; }

		SqlBuilder Sql { get; }

		Task OpenAsync();
		Task CloseAsync();

		void ConvertPropertyTypeToDbType(PropertyInfo propertyInfo, SqlBuilder sqlBuilder);
		object ConvertDbValueToPropertyValue(string dbType, object dbValue, PropertyInfo propertyInfo);
		object ConvertPropertyValueToDbValue(object propertyValue, PropertyInfo propertyInfo);

		//

		Task<int> StatementAsync(string sql, params (string name, object value)[] parameters);
		Task<int> StatementAsync(string sql, params object[] parameters);
		Task<int> StatementAsync(string sql);

		Task<T> ScalarAsync<T>(string sql, params (string name, object value)[] parameters);
		Task<T> ScalarAsync<T>(string sql, params object[] parameters);
		Task<T> ScalarAsync<T>(string sql);

		Task<IQueryResult<T>> QueryAsync<T>(string sql, params (string name, object value)[] parameters);
		Task<IQueryResult<T>> QueryAsync<T>(string sql, params object[] parameters);
		Task<IQueryResult<T>> QueryAsync<T>(string sql);

		//

		// If table not exists then create, otherwise perform upgrade if needed.
		ITable<T> Table<T>(string tableName);
		Task<ITable<T>> TableAsync<T>(string tableName);

		Task DropTableAsync(string tableName);
	}
}
