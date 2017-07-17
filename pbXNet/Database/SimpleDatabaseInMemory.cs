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
		public string Name { get; } = T.Localized("SDIM_Name");

		public SqlBuilder Sql => throw new NotSupportedException();

		public class Exception<T> : System.Exception
		{
			public Exception(string message) : base(message)
			{ }
		}

		class InternalQueryResult<T> : IQueryResult<T>
		{
			IEnumerable<T> _rows;

			public InternalQueryResult(IEnumerable<T> rows)
			{
				_rows = rows;
			}

			public void AddFilter(Func<T, bool> where)
			{
				throw new NotImplementedException();
			}

			public void Dispose()
			{}

			public IEnumerator<T> GetEnumerator() => _rows.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => _rows.GetEnumerator();
		}

		class InternalQuery<T> : IQuery<T>
		{
			IEnumerable<T> _rows;

			public InternalQuery(IEnumerable<T> rows)
			{
				_rows = rows;
			}

			public IQuery<T> Where(Expression<Func<T, bool>> expr) => new InternalQuery<T>(_rows.Where(expr.Compile()));

			public IQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> expr) => new InternalQuery<T>(_rows.OrderBy<T, TKey>(expr.Compile()));

			public IQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> expr) => new InternalQuery<T>(_rows.OrderByDescending<T, TKey>(expr.Compile()));

			public async Task<IQueryResult<T>> PrepareAsync() => new InternalQueryResult<T>(_rows);

			public async Task<bool> AnyAsync() => _rows.Any();

			public void Dispose() { }
		}

		interface IDumpable
		{
			void PrepareDump();
		}

		class InternalTable<T> : ITable<T>, IDumpable
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

			List<PropertyInfo> _primaryKey;

			List<T> _rows;

			SemaphoreSlim _lock;

			public InternalTable(string name)
			{
				Name = name;
				Type = typeof(T);
				_rows = new List<T>(1024);
				_primaryKey = new List<PropertyInfo>();
				_lock = new SemaphoreSlim(1);

				// Create primary key based on [PrimaryKey] attribute.
				_primaryKey.AddRange(
					typeof(T).GetRuntimeProperties()
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

			public Task CreateAsync() => throw new NotSupportedException();
			public Task OpenAsync() => throw new NotSupportedException();

			public async Task<bool> ExistsAsync(T pk)
			{
				return !object.Equals(default(T), await FindAsync(pk).ConfigureAwait(false));
			}

			public async Task<T> FindAsync(T pk)
			{
				if (_primaryKey.Count <= 0)
					throw new Exception<T>(pbXNet.T.Localized("DB_PrimaryKeyNotDefined", Name));

				// These simple optimizations speed up the whole search twice.

				if (_primaryKey.Count == 1)
				{
					PropertyInfo p1 = _primaryKey[0];
					object pk1 = p1.GetValue(pk);
					return _rows.Find((o) => pk1.Equals(p1.GetValue(o)));
				}

				if (_primaryKey.Count == 2)
				{
					PropertyInfo p1 = _primaryKey[0];
					PropertyInfo p2 = _primaryKey[1];
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
					throw new Exception<T>(pbXNet.T.Localized("SDIM_ObjectDoesntExist"));

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

			public async Task DeleteAsync(T o)
			{
				if (_primaryKey.Count <= 0)
				{
					// TODO: delete all that match T o
					return;
				}

				T obj = await FindAsync(o).ConfigureAwait(false);
				if (obj == null || obj.Equals(default(T)))
				{
					// do nothing
				}
				else
				{
					await LockAsync();
					try
					{
						_rows.Remove(obj);
					}
					finally
					{
						Unlock();
					}
				}
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
						Type = p.PropertyType,
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

		public ITable<T> Table<T>(string tableName) => TableAsync<T>(tableName).GetAwaiter().GetResult();

		public async Task<ITable<T>> TableAsync<T>(string tableName)
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

		public void ConvertPropertyTypeToDbType(PropertyInfo propertyInfo, SqlBuilder sqlBuilder) => throw new NotSupportedException();
		public object ConvertDbValueToPropertyValue(string dbType, object dbValue, PropertyInfo propertyInfo) => throw new NotSupportedException();
		public object ConvertPropertyValueToDbValue(object propertyValue, PropertyInfo propertyInfo) => throw new NotSupportedException();

		public async Task OpenAsync() { }
		public async Task CloseAsync() { }

		public Task<int> StatementAsync(string sql, params (string name, object value)[] parameters) => throw new NotSupportedException();
		public Task<int> StatementAsync(string sql, params object[] parameters) => throw new NotSupportedException();
		public Task<int> StatementAsync(string sql) => throw new NotSupportedException();
		public Task<T> ScalarAsync<T>(string sql, params (string name, object value)[] parameters) => throw new NotSupportedException();
		public Task<T> ScalarAsync<T>(string sql, params object[] parameters) => throw new NotSupportedException();
		public Task<T> ScalarAsync<T>(string sql) => throw new NotSupportedException();
		public Task<IQueryResult<T>> QueryAsync<T>(string sql, params (string name, object value)[] parameters) => throw new NotSupportedException();
		public Task<IQueryResult<T>> QueryAsync<T>(string sql, params object[] parameters) => throw new NotSupportedException();
		public Task<IQueryResult<T>> QueryAsync<T>(string sql) => throw new NotSupportedException();
	}
}
