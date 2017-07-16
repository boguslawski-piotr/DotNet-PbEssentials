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

		public SqlBuilder SqlBuilder => throw new NotSupportedException();

		public class Exception<T> : System.Exception
		{
			public Exception(string message) : base(message)
			{ }
		}

		class InternalTableRows<T> : IQueryEx<T>
		{
			IEnumerable<T> _rows;

			public InternalTableRows(IEnumerable<T> rows)
			{
				_rows = rows;
			}

			public IQueryEx<T> Where(Expression<Func<T, bool>> expr) => new InternalTableRows<T>(_rows.Where(expr.Compile()));

			public IQueryEx<T> OrderBy<K>(Expression<Func<T, K>> expr) => new InternalTableRows<T>(_rows.OrderBy<T, K>(expr.Compile()));

			public IQueryEx<T> OrderByDescending<K>(Expression<Func<T, K>> expr) => new InternalTableRows<T>(_rows.OrderByDescending<T, K>(expr.Compile()));

			public async Task<IEnumerable<T>> PrepareAsync() => _rows;

			public async Task<bool> AnyAsync() => _rows.Any();

			public IEnumerator<T> GetEnumerator() => _rows.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _rows.GetEnumerator();

			public void Dispose() { }
		}

		interface IDumpable
		{
			void PrepareDump();
		}

		class InternalTable<T> : ITable<T>, IDumpable
		{
			public string Name;

			public Type Type;

			public IQueryEx<T> Rows => new InternalTableRows<T>(_rows);

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

			public InternalTable(string tableName)
			{
				Name = tableName;
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

			public async Task<T> FindAsync(T pk)
			{
				if (_primaryKey.Count <= 0)
					throw new Exception<T>(pbXNet.T.Localized("SDIM_PrimaryKeyNotDefined", Name));

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

			public async Task InsertAsync(T o)
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
					// TODO: delete all that matching o
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

		public async Task<bool> TableExistsAsync(string tableName)
		{
			return _tables.ContainsKey(tableName);
		}

		public async Task<ITable<T>> CreateTableAsync<T>(string tableName)
		{
			if (_tables.TryGetValue(tableName, out object _t))
				return (InternalTable<T>)_t;

			InternalTable<T> t = new InternalTable<T>(tableName);
			_tables.TryAdd(tableName, t);

			return t;
		}

		public async Task DropTableAsync(string tableName)
		{
			_tables.TryRemove(tableName, out _);
		}

		public async Task<ITable<T>> TableAsync<T>(string tableName)
		{
			return (InternalTable<T>)_tables[tableName];
		}

		public ITable<T> Table<T>(string tableName) => TableAsync<T>(tableName).GetAwaiter().GetResult();

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

		public async Task OpenAsync() { }
		public async Task CloseAsync() { }

		public Task<int> StatementAsync(string sql, params (string name, object value)[] parameters) => throw new NotSupportedException();
		public Task<int> StatementAsync(string sql, params object[] parameters) => throw new NotSupportedException();
		public Task<int> StatementAsync(string sql) => throw new NotSupportedException();
		public Task<T> ScalarAsync<T>(string sql, params (string name, object value)[] parameters) => throw new NotSupportedException();
		public Task<T> ScalarAsync<T>(string sql, params object[] parameters) => throw new NotSupportedException();
		public Task<T> ScalarAsync<T>(string sql) => throw new NotSupportedException();
		public Task<IQuery<T>> QueryAsync<T>(string sql, params (string name, object value)[] parameters) => throw new NotSupportedException();
		public Task<IQuery<T>> QueryAsync<T>(string sql, params object[] parameters) => throw new NotSupportedException();
		public Task<IQuery<T>> QueryAsync<T>(string sql) => throw new NotSupportedException();
	}
}
