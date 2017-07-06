using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace pbXNet
{
	public class SimpleDatabaseInMemory : ISimpleDatabase
	{
		public string Name { get; } = T.Localized("SDIM_Name");

		[AttributeUsage(AttributeTargets.Property)]
		public class PrimaryKeyAttribute : Attribute { }

		interface ITable
		{
			void PrepareDump();
		}

		class Table<T> : ITable
		{
			public string Name;
			public Type Type;

			public class PrimaryKeyInfoField
			{
				public string Name;
				public Type Type;
			}

			public List<PrimaryKeyInfoField> PrimaryKeyInfo;
			[NonSerialized]	public List<PropertyInfo> PrimaryKey;

			public List<T> Rows;

			SemaphoreSlim _lock;

			public Table(string tableName)
			{
				Name = tableName;
				Type = typeof(T);
				Rows = new List<T>(1024);
				PrimaryKey = new List<PropertyInfo>();
				_lock = new SemaphoreSlim(1);
			}

			public async Task LockAsync()
			{
				await _lock.WaitAsync();
			}

			public void Unlock()
			{
				_lock.Release();
			}

			public void PrepareDump()
			{
				PrimaryKeyInfo = new List<PrimaryKeyInfoField>(
					PrimaryKey.Select((p) => new PrimaryKeyInfoField()
					{
						Type = p.PropertyType,
						Name = p.Name,
					})
				);
			}
		}

		public class Exception<T> : System.Exception
		{
			public Exception(string message) : base(message)
			{ }
		}

		ConcurrentDictionary<string, object> _tables = new ConcurrentDictionary<string, object>();

		Table<T> GetTable<T>(string tableName) => (Table<T>)_tables[tableName];

		public async Task InitializeAsync()
		{
		}

		public async Task CreateTableAsync<T>(string tableName)
		{
			if (_tables.ContainsKey(tableName))
				return;

			Table<T> t = new Table<T>(tableName);
			_tables[tableName] = t;

			// Create primary key based on [PrimaryKey] attribute.
			t.PrimaryKey.AddRange(
#if NETSTANDARD1_6
				typeof(T).GetRuntimeProperties()
#else
				typeof(T).GetProperties()
#endif
					.Where((p) => p.CustomAttributes.Any((a) => a.AttributeType.Name == nameof(PrimaryKeyAttribute)))
			);
		}

		public async Task CreatePrimaryKeyAsync<T>(string tableName, params string[] columnNames)
		{
			Table<T> t = GetTable<T>(tableName);

			t.PrimaryKey.Clear();
			t.PrimaryKey.AddRange(
#if NETSTANDARD1_6
				typeof(T).GetRuntimeProperties()
#else
				typeof(T).GetProperties()
#endif
					.Where((p) => columnNames.Contains(p.Name))
			);
		}

		public async Task CreateIndexAsync<T>(string tableName, bool unique, params string[] columnNames)
		{
		}

		public async Task<IEnumerable<T>> SelectAsync<T>(string tableName, Expression<Func<T, bool>> where)
		{
			Table<T> t = GetTable<T>(tableName);
			return t.Rows.Where<T>(where.Compile());
		}

		public async Task<T> FindAsync<T>(string tableName, T pk) => await FindAsync(GetTable<T>(tableName), pk).ConfigureAwait(false);

		async Task<T> FindAsync<T>(Table<T> t, T pk)
		{
			if (t.PrimaryKey.Count <= 0)
				throw new Exception<T>(pbXNet.T.Localized("SDIM_PrimaryKeyNotDefined", t.Name));

			// These simple optimizations speed up the whole search twice.

			if (t.PrimaryKey.Count == 1)
			{
				PropertyInfo p1 = t.PrimaryKey[0];
				object pk1 = p1.GetValue(pk);
				return t.Rows.Find((o) => pk1.Equals(p1.GetValue(o)));
			}

			if (t.PrimaryKey.Count == 2)
			{
				PropertyInfo p1 = t.PrimaryKey[0];
				PropertyInfo p2 = t.PrimaryKey[1];
				object pk1 = p1.GetValue(pk);
				object pk2 = p2.GetValue(pk);
				return t.Rows.Find((o) => pk1.Equals(p1.GetValue(o)) && pk2.Equals(p2.GetValue(o)));
			}

			return t.Rows.Find((o) =>
				t.PrimaryKey.Count == t.PrimaryKey.Count((p) => p.GetValue(pk).Equals(p.GetValue(o)))
			);
		}

		public async Task InsertAsync<T>(string tableName, T o) => await InsertAsync(GetTable<T>(tableName), o).ConfigureAwait(false);

		async Task InsertAsync<T>(Table<T> t, T o)
		{
			T obj = await FindAsync(t, o).ConfigureAwait(false);

			await t.LockAsync();
			try
			{
				if (obj == null || obj.Equals(default(T)))
					t.Rows.Add(o);
				else
				{
					if (!object.ReferenceEquals(obj, o))
						t.Rows[t.Rows.IndexOf(obj)] = o;
				}
			}
			finally
			{
				t.Unlock();
			}
		}

		public async Task UpdateAsync<T>(string tableName, T o) => await UpdateAsync(GetTable<T>(tableName), o).ConfigureAwait(false);

		async Task UpdateAsync<T>(Table<T> t, T o)
		{
			T obj = await FindAsync(t, o).ConfigureAwait(false);
			if (obj == null || obj.Equals(default(T)))
				throw new Exception<T>(pbXNet.T.Localized("SDIM_ObjectDoesntExist"));

			if (!object.ReferenceEquals(obj, o))
			{
				await t.LockAsync();
				try
				{
					t.Rows[t.Rows.IndexOf(obj)] = o;
				}
				finally
				{
					t.Unlock();
				}
			}
		}

		public async Task DeleteAsync<T>(string tableName, T pk) => await DeleteAsync(GetTable<T>(tableName), pk).ConfigureAwait(false);

		async Task DeleteAsync<T>(Table<T> t, T pk)
		{
			T obj = await FindAsync(t, pk).ConfigureAwait(false);
			if (obj == null || obj.Equals(default(T)))
				throw new Exception<T>(pbXNet.T.Localized("SDIM_ObjectDoesntExist"));

			await t.LockAsync();
			try
			{
				t.Rows.Remove(obj);
			}
			finally
			{
				t.Unlock();
			}
		}

		public async Task Dump(string filename, IFileSystem fs)
		{
			foreach (var t in _tables.Values)
			{
				((ITable)t).PrepareDump();
			}

			await fs.WriteTextAsync(filename, new NewtonsoftJsonSerializer().Serialize(_tables)).ConfigureAwait(false);
		}
	}
}
