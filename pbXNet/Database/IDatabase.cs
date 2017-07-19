using System;
using System.Reflection;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public interface IDatabase : IDisposable
	{
		string Name { get; }

		SqlBuilder SqlBuilder { get; }

		IExpressionTranslator ExpressionTranslator { get; }

		Task OpenAsync();
		Task CloseAsync();

		void ConvertPropertyTypeToDbType(PropertyInfo propertyInfo, SqlBuilder sql);
		object ConvertDbValueToPropertyValue(string dbType, object dbValue, PropertyInfo propertyInfo);
		object ConvertPropertyValueToDbValue(object propertyValue, PropertyInfo propertyInfo);

		Task<int> StatementAsync(string sql, params (string name, object value)[] parameters);
		Task<int> StatementAsync(string sql, params object[] parameters);
		Task<int> StatementAsync(string sql);

		Task<T> ScalarAsync<T>(string sql, params (string name, object value)[] parameters);
		Task<T> ScalarAsync<T>(string sql, params object[] parameters);
		Task<T> ScalarAsync<T>(string sql);

		Task<IQueryResult<T>> QueryAsync<T>(string sql, params (string name, object value)[] parameters) where T : new();
		Task<IQueryResult<T>> QueryAsync<T>(string sql, params object[] parameters) where T : new();
		Task<IQueryResult<T>> QueryAsync<T>(string sql) where T : new();

		IQuery<T> Query<T>(string tableName) where T : new();
		IQuery<T> Query<T>(SqlBuilder sqlBuilder) where T : new();

		// If table not exists then create, otherwise perform upgrade if needed.
		ITable<T> Table<T>(string tableName) where T : new();
		Task<ITable<T>> TableAsync<T>(string tableName) where T : new();

		Task DropTableAsync(string tableName);
	}
}
