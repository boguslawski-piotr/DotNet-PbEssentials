using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public class SDCDatabase : IDatabase
	{
		public string Name => $"{_db.DataSource}/{_db.Database}";

		public SqlBuilder SqlBuilder => _options.SqlBuilder;

		public IExpressionTranslator ExpressionTranslator => _options.ExpressionTranslator;

		public class Options
		{
			public SqlBuilder SqlBuilder;
			public IExpressionTranslator ExpressionTranslator;
		}

		protected Options _options;

		protected DbConnection _db;

		bool _dbOpenedHere;

		public SDCDatabase(DbConnection db, Options options = null)
		{
			Check.Null(db, nameof(db));

			_db = db;
			_options = options ?? new Options();

			if (_options.SqlBuilder == null)
				_options.SqlBuilder = new SqlBuilder();
			if (_options.ExpressionTranslator == null)
				_options.ExpressionTranslator = new Expression2SqlTranslator(_options.SqlBuilder.New(), null);
		}

		public virtual void Dispose()
		{
			if (_dbOpenedHere)
				_db?.Close();

			_db = null;
			_dbOpenedHere = false;
		}

		public virtual async Task OpenAsync()
		{
			if (_db.State == System.Data.ConnectionState.Broken)
				_db.Close();
			if (_db.State == System.Data.ConnectionState.Closed)
			{
				await _db.OpenAsync().ConfigureAwait(false);
				_dbOpenedHere = true;

				Log.I($"opened connection to database '{_db.DataSource}/{_db.Database}'.", this);
			}
			if (_db.State != System.Data.ConnectionState.Open)
			{
				await Task.Delay(1000).ConfigureAwait(false);
				if (_db.State != System.Data.ConnectionState.Open)
					throw new Exception($"Unable to connect to database '{_db.DataSource}/{_db.Database}'.");
			}
		}

		public virtual async Task CloseAsync()
		{
			if (_dbOpenedHere)
				_db.Close();
			_dbOpenedHere = false;
		}

		public virtual void ConvertPropertyTypeToDbType(PropertyInfo propertyInfo, SqlBuilder sql)
		{
			Type type = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
			TypeCode typeCode = Type.GetTypeCode(type);
			switch (typeCode)
			{
				case TypeCode.String:
					var attrs = propertyInfo.CustomAttributes.ToList();
					int width = (int)(attrs.Find(a => a.AttributeType.Name == nameof(LengthAttribute))?.ConstructorArguments[0].Value ?? int.MaxValue);
					if (width == int.MaxValue)
						sql.NText();
					else
						sql.NVarchar(width);
					break;

				case TypeCode.Boolean:
					sql.Boolean();
					break;

				case TypeCode.Int16:
				case TypeCode.UInt16:
					sql.Smallint();
					break;

				case TypeCode.Int32:
				case TypeCode.UInt32:
					sql.Int();
					break;

				case TypeCode.Int64:
				case TypeCode.UInt64:
					sql.Bigint();
					break;

				// TODO: handle more conversion from C# type to SQL type

				default:
					throw new Exception($"Unsupported type '{propertyInfo.PropertyType}'."); // TODO: localization and better text
			}
		}

		protected virtual object ConvertDbValueToValue(string dbType, object dbValue, Type type)
		{
			Check.Null(type, nameof(type));

			type = Nullable.GetUnderlyingType(type) ?? type;

			// dbType CAN BE null -> caller don't know this information
			// TODO: handle null, but how?
			var v = Convert.ChangeType(dbValue, type);
			return v;
		}

		public virtual object ConvertDbValueToPropertyValue(string dbType, object dbValue, PropertyInfo propertyInfo)
		{
			Check.Null(propertyInfo, nameof(propertyInfo));

			return ConvertDbValueToValue(dbType, dbValue, propertyInfo.PropertyType);
		}

		public virtual object ConvertPropertyValueToDbValue(object propertyValue, PropertyInfo propertyInfo)
		{
			Check.Null(propertyInfo, nameof(propertyInfo));

			if (propertyValue == null)
				return DBNull.Value;

			// TODO: handle conversions

			return propertyValue;
		}

		protected enum CommandType
		{
			Statement,
			Scalar,
			Query,
			Table,
		};

#if DEBUG
		void DumpParameters(DbCommand cmd, [CallerMemberName]string callerName = null)
		{
			string s = "";
			foreach (DbParameter p in cmd.Parameters)
				s += (s == "" ? "" : ", ") +
					$"@{p.ParameterName} = {{{p.Value.ToString()}}}";
			Log.D(s, this, callerName);
		}
#endif

		protected virtual void CreateParameters(DbCommand cmd, params (string name, object value)[] parameters)
		{
			Check.Null(cmd, nameof(cmd));

			if (parameters == null)
				return;

			foreach (var _p in parameters)
			{
				DbParameter p = cmd.CreateParameter();
				p.ParameterName = _p.name;
				p.Value = _p.value ?? DBNull.Value;
				cmd.Parameters.Add(p);
			}
#if DEBUG
			DumpParameters(cmd);
#endif
		}

		protected DbCommand CreateCommand(CommandType type, string sql, params (string name, object value)[] parameters)
		{
			DbCommand cmd = CreateCommand(type, sql);
			CreateParameters(cmd, parameters);
			return cmd;
		}

		protected DbCommand CreateCommand(CommandType type, string sql, params object[] parameters)
		{
			if (parameters == null)
				return CreateCommand(type, sql);

			(string name, object value)[] _parameters = new(string name, object value)[parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
				_parameters[i] = ($"_{i + 1}", parameters[i]);

			return CreateCommand(type, sql, _parameters);
		}

		protected virtual DbCommand CreateCommand(CommandType type, string sql)
		{
			Check.Empty(sql, nameof(sql));

			DbCommand cmd = _db.CreateCommand();

			cmd.CommandText = sql;

			if (type == CommandType.Table)
			{
				cmd.CommandType = System.Data.CommandType.TableDirect;
			}
			else
			{
				cmd.CommandType = System.Data.CommandType.Text;
				cmd.CommandText += ";";
			}

			Log.D($"{type}: {sql}", this);

			return cmd;
		}

		protected async Task<T> ExecuteCommandAsync<T>(CommandType type, string sql, params (string name, object value)[] parameters) => await ExecuteCommandAsync<T>(type, CreateCommand(type, sql, parameters), true).ConfigureAwait(false);

		protected async Task<T> ExecuteCommandAsync<T>(CommandType type, string sql, params object[] parameters) => await ExecuteCommandAsync<T>(type, CreateCommand(type, sql, parameters), true).ConfigureAwait(false);

		protected async Task<T> ExecuteCommandAsync<T>(CommandType type, string sql) => await ExecuteCommandAsync<T>(type, CreateCommand(type, sql), true).ConfigureAwait(false);

		protected async Task<T> ExecuteCommandAsync<T>(CommandType type, DbCommand cmd) => await ExecuteCommandAsync<T>(type, cmd, false).ConfigureAwait(false);

		protected virtual async Task<T> ExecuteCommandAsync<T>(CommandType type, DbCommand cmd, bool shouldDisposeCmd)
		{
			Check.Null(cmd, nameof(cmd));
			Check.True(type == CommandType.Statement || type == CommandType.Scalar, nameof(type));

			await OpenAsync().ConfigureAwait(false);

			try
			{
				object v = null;

				if (type == CommandType.Statement)
					v = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
				else if (type == CommandType.Scalar)
					v = await cmd.ExecuteScalarAsync().ConfigureAwait(false);

				return (T)ConvertDbValueToValue(null, v, typeof(T));
				//return (T)Convert.ChangeType(v, typeof(T));
			}
			finally
			{
				if (shouldDisposeCmd)
					cmd.Dispose();
			}
		}

		protected virtual async Task<IQueryResult<T>> ExecuteReaderAsync<T>(DbCommand cmd, bool shouldDisposeCmd) where T : new()
		{
			Check.Null(cmd, nameof(cmd));

			await OpenAsync().ConfigureAwait(false);

			return new SDCQueryResult<T>(this, shouldDisposeCmd ? cmd : null, await cmd.ExecuteReaderAsync().ConfigureAwait(false));
		}

		public async Task<int> StatementAsync(string sql, params (string name, object value)[] parameters) => await ExecuteCommandAsync<int>(CommandType.Statement, sql, parameters).ConfigureAwait(false);

		public async Task<int> StatementAsync(string sql, params object[] parameters) => await ExecuteCommandAsync<int>(CommandType.Statement, sql, parameters).ConfigureAwait(false);

		public async Task<int> StatementAsync(string sql) => await ExecuteCommandAsync<int>(CommandType.Statement, sql).ConfigureAwait(false);

		public async Task<T> ScalarAsync<T>(string sql, params (string name, object value)[] parameters) => await ExecuteCommandAsync<T>(CommandType.Scalar, sql, parameters).ConfigureAwait(false);

		public async Task<T> ScalarAsync<T>(string sql, params object[] parameters) => await ExecuteCommandAsync<T>(CommandType.Scalar, sql, parameters).ConfigureAwait(false);

		public async Task<T> ScalarAsync<T>(string sql) => await ExecuteCommandAsync<T>(CommandType.Scalar, sql).ConfigureAwait(false);

		public async Task<IQueryResult<T>> QueryAsync<T>(string sql, params (string name, object value)[] parameters) where T : new()
			=> await ExecuteReaderAsync<T>(CreateCommand(CommandType.Query, sql, parameters), true).ConfigureAwait(false);

		public async Task<IQueryResult<T>> QueryAsync<T>(string sql, params object[] parameters) where T : new()
			=> await ExecuteReaderAsync<T>(CreateCommand(CommandType.Query, sql, parameters), true).ConfigureAwait(false);

		public async Task<IQueryResult<T>> QueryAsync<T>(string sql) where T : new()
			=> await ExecuteReaderAsync<T>(CreateCommand(CommandType.Query, sql), true).ConfigureAwait(false);

		//

		public virtual IQuery<T> Query<T>(string tableName) where T : new() => new SqlQuery<T>(this, tableName);

		public virtual IQuery<T> Query<T>(SqlBuilder sqlBuilder) where T : new() => new SqlQuery<T>(this, sqlBuilder);

		public ITable<T> Table<T>(string tableName) where T : new() => TableAsync<T>(tableName).GetAwaiter().GetResult();

		public virtual async Task<ITable<T>> TableAsync<T>(string tableName) where T : new()
		{
			Check.Empty(tableName, nameof(tableName));

			// TODO: dictionary...

			// TODO: better method to find if table exists?
			try
			{
				await ScalarAsync<object>(SqlBuilder.Select().E("1").From(tableName)).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				return await SqlTable<T>.CreateAsync(this, tableName);
			}

			return await SqlTable<T>.OpenAsync(this, tableName);
		}

		public virtual async Task DropTableAsync(string tableName)
		{
			Check.Empty(tableName, nameof(tableName));

			await StatementAsync(SqlBuilder.Drop().Table(tableName)).ConfigureAwait(false);
		}
	}
}
