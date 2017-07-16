using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public class SDCDatabase : IDatabase
	{
		public string Name => $"{_db.DataSource}/{_db.Database}";

		public SqlBuilder SqlBuilder { get; private set; }

		public class Options { }

		Options _options;
		DbConnection _db;
		bool _closeDb;

		public SDCDatabase(DbConnection db, SqlBuilder sqlBuilder = null, Options options = null)
		{
			SqlBuilder = sqlBuilder ?? new SqlBuilder();
			_options = options ?? new Options();
			_db = db;
		}

		public virtual void Dispose()
		{
			if (_closeDb)
				_db?.Close();

			_db = null;
			_closeDb = false;
		}

		public virtual async Task OpenAsync()
		{
			if (_db.State == System.Data.ConnectionState.Broken)
				_db.Close();
			if (_db.State == System.Data.ConnectionState.Closed)
			{
				await _db.OpenAsync().ConfigureAwait(false);
				_closeDb = true;

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
			if (_closeDb)
				_db.Close();
			_closeDb = false;
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
			foreach (var _p in parameters)
			{
				DbParameter p = cmd.CreateParameter();
				p.ParameterName = _p.name;
				p.Value = _p.value;
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
			(string name, object value)[] _parameters = new(string name, object value)[parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
				_parameters[i] = ($"_{i + 1}", parameters[i]);

			return CreateCommand(type, sql, _parameters);
		}

		protected virtual DbCommand CreateCommand(CommandType type, string sql)
		{
			DbCommand cmd = _db.CreateCommand();

			cmd.CommandText = sql + ";";

			if (type == CommandType.Table)
				cmd.CommandType = System.Data.CommandType.TableDirect;
			else
				cmd.CommandType = System.Data.CommandType.Text;

#if DEBUG
			sql = Regex.Replace(sql, "\\s+", " ");
			sql = Regex.Replace(sql, "\\s+\\)", ")");
			sql = Regex.Replace(sql, "[\\s\\(\\)]+,", ",");
			sql = Regex.Replace(sql, ",[\\s\\(\\)]+", ",");
#endif
			Log.D($"{type}: {sql}", this);

			return cmd;
		}

		public class Query<T> : IQuery<T>
		{
			DbDataReader _rows;
			DbCommand _cmd;

			public Query(DbCommand cmd, DbDataReader rows)
			{
				_rows = rows;
				_cmd = cmd;
			}

			public void Dispose()
			{
				_rows?.Dispose();
				_rows = null;
				_cmd?.Dispose();
				_cmd = null;
			}

			public IEnumerator GetEnumerator() => new Enumerator<T>(_rows);

			IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator<T>(_rows);

			public class Enumerator<T> : IEnumerator<T>
			{
				IDictionary<string, PropertyInfo> _properties;
				IEnumerator _enumerator;
				T _current = default(T);

				public Enumerator(DbDataReader rows)
				{
					_enumerator = rows.GetEnumerator();

					Type t = typeof(T);
					if (t.IsClass)
						_current = Activator.CreateInstance<T>();

					_properties = t.GetRuntimeProperties().ToDictionary(_p => _p.Name);
				}

				void IDisposable.Dispose() { }

				public bool MoveNext()
				{
					if (!_enumerator.MoveNext())
						return false;

					try
					{
						DbDataRecord r = (DbDataRecord)_enumerator.Current;
						for (int i = 0; i < r.FieldCount; i++)
						{
							var p = _properties[r.GetName(i)];
							// TODO: handle r.IsDBNull, but how?
							var v = Convert.ChangeType(r.GetValue(i), p.PropertyType);
							p.SetValue(_current, v);
						}
					}
					catch (Exception ex)
					{
						Log.E(ex, this);
						throw ex;
					}

					return true;
				}

				public void Reset() => _enumerator.Reset();

				public T Current => _current;

				object IEnumerator.Current => Current;
			}
		}

		protected async Task<T> ExecuteCommandAsync<T>(CommandType type, string sql, params (string name, object value)[] parameters) => await ExecuteCommandAsync<T>(type, CreateCommand(type, sql, parameters), true).ConfigureAwait(false);
		protected async Task<T> ExecuteCommandAsync<T>(CommandType type, string sql, params object[] parameters) => await ExecuteCommandAsync<T>(type, CreateCommand(type, sql, parameters), true).ConfigureAwait(false);
		protected async Task<T> ExecuteCommandAsync<T>(CommandType type, string sql) => await ExecuteCommandAsync<T>(type, CreateCommand(type, sql), true).ConfigureAwait(false);
		protected async Task<T> ExecuteCommandAsync<T>(CommandType type, DbCommand cmd) => await ExecuteCommandAsync<T>(type, cmd, false).ConfigureAwait(false);

		protected virtual async Task<T> ExecuteCommandAsync<T>(CommandType type, DbCommand cmd, bool shouldDisposeCmd)
		{
			await OpenAsync().ConfigureAwait(false);
			object v = null;
			try
			{
				if (type == CommandType.Statement)
				{
					v = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
				}
				else if (type == CommandType.Scalar)
					v = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
				else
					throw new Exception("Invalid command type."); // TODO: translation and better text

				return (T)Convert.ChangeType(v, typeof(T));
			}
			finally
			{
				if (shouldDisposeCmd)
					cmd.Dispose();
			}
		}

		protected virtual async Task<IQuery<T>> ExecuteReaderAsync<T>(DbCommand cmd, bool shouldDisposeCmd)
		{
			await OpenAsync().ConfigureAwait(false);
			return new Query<T>(shouldDisposeCmd ? cmd : null, await cmd.ExecuteReaderAsync().ConfigureAwait(false));
		}

		public async Task<int> StatementAsync(string sql, params (string name, object value)[] parameters) => await ExecuteCommandAsync<int>(CommandType.Statement, sql, parameters).ConfigureAwait(false);
		public async Task<int> StatementAsync(string sql, params object[] parameters) => await ExecuteCommandAsync<int>(CommandType.Statement, sql, parameters).ConfigureAwait(false);
		public async Task<int> StatementAsync(string sql) => await ExecuteCommandAsync<int>(CommandType.Statement, sql).ConfigureAwait(false);

		public async Task<T> ScalarAsync<T>(string sql, params (string name, object value)[] parameters) => await ExecuteCommandAsync<T>(CommandType.Scalar, sql, parameters).ConfigureAwait(false);
		public async Task<T> ScalarAsync<T>(string sql, params object[] parameters) => await ExecuteCommandAsync<T>(CommandType.Scalar, sql, parameters).ConfigureAwait(false);
		public async Task<T> ScalarAsync<T>(string sql) => await ExecuteCommandAsync<T>(CommandType.Scalar, sql).ConfigureAwait(false);

		public async Task<IQuery<T>> QueryAsync<T>(string sql, params (string name, object value)[] parameters) => await ExecuteReaderAsync<T>(CreateCommand(CommandType.Query, sql, parameters), true).ConfigureAwait(false);
		public async Task<IQuery<T>> QueryAsync<T>(string sql, params object[] parameters) => await ExecuteReaderAsync<T>(CreateCommand(CommandType.Query, sql, parameters), true).ConfigureAwait(false);
		public async Task<IQuery<T>> QueryAsync<T>(string sql) => await ExecuteReaderAsync<T>(CreateCommand(CommandType.Query, sql), true).ConfigureAwait(false);

		//

		public async Task<bool> TableExistsAsync(string tableName)
		{
			bool exists = true;
			try
			{
				await ScalarAsync<object>(SqlBuilder.Select.E("1").From(tableName)).ConfigureAwait(false);
			}
			catch
			{
				exists = false;
			}
			return exists;
		}

		public async Task<ITable<T>> CreateTableAsync<T>(string tableName)
		{
			if (string.IsNullOrWhiteSpace(tableName))
				throw new ArgumentNullException(nameof(tableName));

			if (await TableExistsAsync(tableName).ConfigureAwait(false))
			{
				// TODO: perform table upgrade/downgrade if nedded...
				return await TableAsync<T>(tableName);
			}

			var properties = typeof(T).GetRuntimeProperties();

			IList<string> primaryKey =
				properties
					.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == nameof(PrimaryKeyAttribute)))
					.Select(p => p.Name).ToList();

			IDictionary<string, List<(string cn, bool desc)>> indexes = new Dictionary<string, List<(string cn, bool desc)>>();

			SqlBuilder.Create.Table(tableName);
			foreach (var p in properties)
			{
				_ = SqlBuilder.C(p.Name);

				var attrs = p.CustomAttributes.ToList();

				bool notNull =
					primaryKey.Contains(p.Name) ||
					attrs.Find(a => a.AttributeType.Name == nameof(NotNullAttribute)) != null;

				CustomAttributeData indexAttr = attrs.Find(a => a.AttributeType.Name == nameof(IndexAttribute));
				if (indexAttr != null)
				{
					string indexName = (string)indexAttr.ConstructorArguments[0].Value;
					if (!indexes.TryGetValue(indexName, out List<(string, bool)> indexColumns))
						indexColumns = new List<(string cn, bool desc)>();

					indexColumns.Add((p.Name, (bool)indexAttr.ConstructorArguments[1].Value));
					indexes[indexName] = indexColumns;
				}

				switch (Type.GetTypeCode(p.PropertyType))
				{
					case TypeCode.String:
						int width = (int)(attrs.Find(a => a.AttributeType.Name == nameof(WidthAttribute))?.ConstructorArguments[0].Value ?? int.MaxValue);
						if (width == int.MaxValue)
							_ = SqlBuilder.NText;
						else
							_ = SqlBuilder.NVarchar(width);
						break;

					case TypeCode.Boolean:
						_ = SqlBuilder.Boolean;
						break;

					case TypeCode.Int16:
						_ = SqlBuilder.Smallint;
						break;

					case TypeCode.Int32:
					case TypeCode.UInt16:
						_ = SqlBuilder.Int;
						break;

					case TypeCode.Int64:
					case TypeCode.UInt32:
						_ = SqlBuilder.Bigint;
						break;

					default:
						throw new Exception("Unsupported type {p.PropertyType}."); // TODO: localization and better text
				}

				_ = notNull ? SqlBuilder.NotNull : SqlBuilder.Null;
			}

			if (primaryKey.Count > 0)
			{
				_ = SqlBuilder.Constraint($"PK_{tableName}").PrimaryKey;
				foreach (var pkc in primaryKey)
					_ = SqlBuilder.C(pkc);
			}

			await StatementAsync(SqlBuilder.Build()).ConfigureAwait(false);

			foreach (var index in indexes)
			{
				string indexName = $"IX_{tableName}_{index.Key}";

				await StatementAsync(
					SqlBuilder.Drop.Index(indexName).On(tableName)
				).ConfigureAwait(false);

				SqlBuilder.Create.Index(indexName).On(tableName);
				foreach (var indexColumn in index.Value)
				{
					_ = SqlBuilder.C(indexColumn.cn);
					_ = indexColumn.desc ? SqlBuilder.Desc : null;
				}

				await StatementAsync(SqlBuilder.Build()).ConfigureAwait(false);
			}

			return await TableAsync<T>(tableName);
		}

		public async Task DropTableAsync(string tableName)
		{
			await StatementAsync(SqlBuilder.Drop.Table(tableName)).ConfigureAwait(false);
		}

		public ITable<T> Table<T>(string tableName) => TableAsync<T>(tableName).GetAwaiter().GetResult();

		public async Task<ITable<T>> TableAsync<T>(string tableName)
		{
			await OpenAsync().ConfigureAwait(false);

			return null;
		}
	}
}
