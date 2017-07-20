using System;
using System.Reflection;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public enum ConnectionType
	{
		Local,
		Remote,
	}

	public interface IDatabase : IDisposable
	{
		string Name { get; }

		ConnectionType ConnectionType { get; }

		SqlBuilder SqlBuilder { get; }

		IExpressionTranslator ExpressionTranslator { get; }

		Task OpenAsync();

		Task CloseAsync();

		string ConvertTypeToDbType(Type type, int width = int.MaxValue);
		
		// dbType CAN BE null -> caller don't know this information
		object ConvertDbValueToValue(string dbType, object dbValue, Type valueType);

		// dbType CAN BE null -> caller don't know this information
		object ConvertValueToDbValue(Type type, object value, string dbType);

		Task<int> StatementAsync(string sql, params (string name, object value)[] parameters);
		Task<int> StatementAsync(string sql, params object[] parameters);
		Task<int> StatementAsync(string sql);

		Task<T> ScalarAsync<T>(string sql, params (string name, object value)[] parameters);
		Task<T> ScalarAsync<T>(string sql, params object[] parameters);
		Task<T> ScalarAsync<T>(string sql);

		Task<IQueryResult<T>> QueryAsync<T>(string sql, params (string name, object value)[] parameters) where T : new();
		Task<IQueryResult<T>> QueryAsync<T>(string sql, params object[] parameters) where T : new();
		Task<IQueryResult<T>> QueryAsync<T>(string sql) where T : new();

		IQuery<T> Query<T>(string sql) where T : new();

		// If table not exists then create, otherwise perform upgrade if needed.
		ITable<T> Table<T>(string tableName, bool createIfNotExists = true) where T : new();
		Task<ITable<T>> TableAsync<T>(string tableName, bool createIfNotExists = true) where T : new();

		Task DropTableAsync(string tableName);
	}
}
