using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public class SimpleDatabaseInMemory : IDatabase
	{
		public ConnectionType ConnectionType { get; } = ConnectionType.Local;

		public string Name { get; } = Localized.T("SDIM_Name");

		public SqlBuilder GetSqlBuilder() => throw new NotSupportedException();

		public IExpressionTranslator GetExpressionTranslator(Type typeForWhichMemberNamesWillBeEmitted = null) => throw new NotSupportedException();

		public class Exception<T> : System.Exception
		{
			public Exception(string message) : base(message)
			{ }
		}

		class InternalQueryResult<T> : IQueryResult<T> where T : new()
		{
			IEnumerable<T> _rows;

			public InternalQueryResult(IEnumerable<T> rows)
			{
				_rows = rows;
			}

			public IQueryResult<T> Where(Func<T, bool> predicate) => new InternalQueryResult<T>(_rows.Where(predicate));

			public IQueryResult<T> OrderBy(Func<T, object> keySelector)
				=> new InternalQueryResult<T>(_rows is IOrderedEnumerable<T> ? ((IOrderedEnumerable<T>)_rows).ThenBy(keySelector) : _rows.OrderBy(keySelector));

			public IQueryResult<T> OrderByDescending(Func<T, object> keySelector)
				=> new InternalQueryResult<T>(_rows is IOrderedEnumerable<T> ? ((IOrderedEnumerable<T>)_rows).ThenByDescending(keySelector) : _rows.OrderByDescending(keySelector));

			public void Dispose()
			{ }

			public IEnumerator<T> GetEnumerator() => _rows.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => _rows.GetEnumerator();
		}

		class InternalQuery<T> : IQuery<T> where T : new()
		{
			IEnumerable<T> _rows;

			public InternalQuery(IEnumerable<T> rows)
			{
				_rows = rows;
			}

			public IQuery<T> Where(Expression<Func<T, bool>> predicate) => new InternalQuery<T>(_rows.Where(predicate.Compile()));

			public IQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
				=> new InternalQuery<T>(_rows is IOrderedEnumerable<T> ? ((IOrderedEnumerable<T>)_rows).ThenBy(keySelector.Compile()) : _rows.OrderBy(keySelector.Compile()));

			public IQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
				=> new InternalQuery<T>(_rows is IOrderedEnumerable<T> ? ((IOrderedEnumerable<T>)_rows).ThenByDescending(keySelector.Compile()) : _rows.OrderByDescending(keySelector.Compile()));

			public async Task<IQueryResult<T>> ResultAsync() => new InternalQueryResult<T>(_rows);

			public async Task<bool> AnyAsync() => _rows.Any();

			public async Task<int> CountAsync() => _rows.Count();

			public void Dispose()
			{ }
		}

		interface IDumpable
		{
			void PrepareDump();
		}

		class InternalTable<T> : ITable<T>, IDumpable where T : new()
		{
			public string Name { get; private set; }

			public Type Type;

			public IQuery<T> Rows => new InternalQuery<T>(_rows);

#if DEBUG
			public class PrimaryKeyInfoField
			{
				public string Name;
				public Type Type;
			}

			public List<PrimaryKeyInfoField> PrimaryKeyInfo;
#endif

			List<MemberInfo> _primaryKey;

			List<T> _rows;

			SemaphoreSlim _lock;

			public InternalTable(string name)
			{
				Name = name;
				Type = typeof(T);
				_rows = new List<T>(1024);
				_primaryKey = new List<MemberInfo>();
				_lock = new SemaphoreSlim(1);

				// Create primary key based on [PrimaryKey] attribute.
				_primaryKey.AddRange(
					typeof(T).GetRuntimePropertiesAndFields()
						.Where((p) => p.CustomAttributes.Any((a) => a.AttributeType.Name == nameof(PrimaryKeyAttribute)))
				);
			}

			public void Dispose()
			{
				_primaryKey?.Clear();
				_primaryKey = null;
				_rows?.Clear();
				_rows = null;
			}

			public async Task CreateAsync() { }
			public async Task OpenAsync() { }

			public async Task<bool> ExistsAsync(T pk)
			{
				return !object.Equals(default(T), await FindAsync(pk).ConfigureAwait(false));
			}

			public async Task<T> FindAsync(T pk)
			{
				if (_primaryKey.Count <= 0)
					throw new Exception<T>(pbXNet.Localized.T("DB_PrimaryKeyNotDefined", Name));

				// These simple optimizations speed up the whole search twice.

				if (_primaryKey.Count == 1)
				{
					MemberInfo p1 = _primaryKey[0];
					object pk1 = p1.GetValue(pk);
					return _rows.Find((o) => pk1.Equals(p1.GetValue(o)));
				}

				if (_primaryKey.Count == 2)
				{
					MemberInfo p1 = _primaryKey[0];
					MemberInfo p2 = _primaryKey[1];
					object pk1 = p1.GetValue(pk);
					object pk2 = p2.GetValue(pk);
					return _rows.Find((o) => pk1.Equals(p1.GetValue(o)) && pk2.Equals(p2.GetValue(o)));
				}

				return _rows.Find((o) =>
					_primaryKey.Count == _primaryKey.Count((p) => p.GetValue(pk).Equals(p.GetValue(o)))
				);
			}

			public async Task InsertOrUpdateAsync(T o)
			{
				T obj = default(T);

				if (_primaryKey.Count > 0)
					obj = await FindAsync(o).ConfigureAwait(false);

				await LockAsync();
				try
				{
					if (obj == null || obj.Equals(default(T)))
					{
						_rows.Add(o);
					}
					else
					{
						if (!object.ReferenceEquals(obj, o))
							_rows[_rows.IndexOf(obj)] = o;
					}
				}
				finally
				{
					Unlock();
				}
			}

			public async Task UpdateAsync(T o)
			{
				T obj = await FindAsync(o).ConfigureAwait(false);
				if (obj == null || obj.Equals(default(T)))
					throw new Exception<T>(pbXNet.Localized.T("SDIM_ObjectDoesntExist"));

				if (!object.ReferenceEquals(obj, o))
				{
					await LockAsync();
					try
					{
						_rows[_rows.IndexOf(obj)] = o;
					}
					finally
					{
						Unlock();
					}
				}
			}

			public async Task<int> UpdateAsync<TA>(TA o, Expression<Func<TA, bool>> predicate)
			{
				throw new NotImplementedException();
			}

			public async Task<int> DeleteAsync(T o)
			{
				if (_primaryKey.Count <= 0)
				{
					// TODO: delete all that match T o
					throw new NotImplementedException();
				}

				T obj = await FindAsync(o).ConfigureAwait(false);
				if (obj == null || obj.Equals(default(T)))
				{
					// do nothing
					return 0;
				}
				else
				{
					await LockAsync();
					try
					{
						_rows.Remove(obj);
						return 1;
					}
					finally
					{
						Unlock();
					}
				}
			}

			public async Task<int> DeleteAsync(Expression<Func<T, bool>> predicate)
			{
				var rowsToDelete = _rows.Where(predicate.Compile()).ToList();
				if (rowsToDelete != null)
				{
					await LockAsync();
					try
					{
						foreach (var obj in rowsToDelete)
						{
							_rows.Remove(obj);
						}

						return rowsToDelete.Count();
					}
					finally
					{
						Unlock();
					}
				}

				return 0;
			}

			async Task LockAsync()
			{
				await _lock.WaitAsync();
			}

			void Unlock()
			{
				_lock.Release();
			}

			public void PrepareDump()
			{
#if DEBUG
				PrimaryKeyInfo = new List<PrimaryKeyInfoField>(
					_primaryKey.Select((p) => new PrimaryKeyInfoField()
					{
						Type = p.GetPropertyOrFieldType(),
						Name = p.Name,
					})
				);
#endif
			}
		}

		ConcurrentDictionary<string, object> _tables = new ConcurrentDictionary<string, object>();

		public void Dispose()
		{
			if (_tables != null)
			{
				foreach (var t in _tables)
					((IDisposable)t.Value)?.Dispose();
			}

			_tables?.Clear();
			_tables = null;
		}

		public IQuery<T> Query<T>(string sql) where T : new() => throw new NotSupportedException();

		public ITable<T> Table<T>(string tableName, bool createIfNotExists = true) where T : new() => TableAsync<T>(tableName, createIfNotExists).GetAwaiter().GetResult();

		public async Task<ITable<T>> TableAsync<T>(string tableName, bool createIfNotExists = true) where T : new()
		{
			if (_tables.TryGetValue(tableName, out object _t))
				return (InternalTable<T>)_t;

			InternalTable<T> t = new InternalTable<T>(tableName);
			_tables.TryAdd(tableName, t);

			return t;
		}

		public async Task DropTableAsync(string tableName)
		{
			if (_tables.TryRemove(tableName, out object t))
				((IDisposable)t).Dispose();
		}

#if DEBUG
		public async Task Dump(string filename, IFileSystem fs)
		{
			foreach (var t in _tables.Values)
			{
				((IDumpable)t).PrepareDump();
			}

			await fs.WriteTextAsync(filename, new NewtonsoftJsonSerializer().Serialize(_tables)).ConfigureAwait(false);
		}
#endif

		public string ConvertTypeToDbType(Type type, int width = int.MaxValue) => throw new NotSupportedException();

		public object ConvertDbValueToValue(string dbType, object dbValue, Type valueType) => dbValue;
		public object ConvertValueToDbValue(Type type, object value, string dbType) => value;

		public async Task OpenAsync() { }
		public async Task CloseAsync() { }

		public Task<int> StatementAsync(string sql, params (string name, object value)[] parameters) => throw new NotSupportedException();
		public Task<int> StatementAsync(string sql, params object[] parameters) => throw new NotSupportedException();
		public Task<int> StatementAsync(string sql) => throw new NotSupportedException();

		public Task<T> ScalarAsync<T>(string sql, params (string name, object value)[] parameters) => throw new NotSupportedException();
		public Task<T> ScalarAsync<T>(string sql, params object[] parameters) => throw new NotSupportedException();
		public Task<T> ScalarAsync<T>(string sql) => throw new NotSupportedException();

		public Task<IQueryResult<T>> QueryAsync<T>(string sql, params (string name, object value)[] parameters) where T : new() => throw new NotSupportedException();
		public Task<IQueryResult<T>> QueryAsync<T>(string sql, params object[] parameters) where T : new() => throw new NotSupportedException();
		public Task<IQueryResult<T>> QueryAsync<T>(string sql) where T : new() => throw new NotSupportedException();
	}
}
