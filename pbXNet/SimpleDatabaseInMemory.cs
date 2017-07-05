using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace pbXNet
{
	public class SimpleDatabaseInMemory : ISimpleDatabase
	{
		public string Name { get; } = T.Localized("SDIM_Name");

		[AttributeUsage(AttributeTargets.Property)]
		public class PrimaryKeyAttribute : Attribute { }

		public class Table<T>
		{
			public string Name;
			public Type Type;
			public List<T> Rows;
			public List<PropertyInfo> PrimaryKey;
		}

		public class Exception<T> : System.Exception
		{
			public Exception(string message, Table<T> t, T o) : base(message)
			{ }
		}

		Dictionary<string, object> _tables = new Dictionary<string, object>();

		Table<T> GetTable<T>(string tableName) => (Table<T>)_tables[tableName];

		public async Task InitializeAsync()
		{
		}

		public async Task CreateTableAsync<T>(string tableName)
		{
			if (_tables.ContainsKey(tableName))
				return;

			Table<T> t = new Table<T>
			{
				Name = tableName,
				Type = typeof(T),
				Rows = new List<T>(),
				PrimaryKey = new List<PropertyInfo>(),
			};

			_tables[tableName] = t;

			// Create primary key based on [PrimaryKey] attribute.
			t.PrimaryKey.AddRange(
				typeof(T).GetProperties()
					.Where((p) => p.CustomAttributes.Any((a) => a.AttributeType.Name == nameof(PrimaryKeyAttribute)))
			);
		}

		public async Task CreatePrimaryKeyAsync<T>(string tableName, params string[] columnNames)
		{
			Table<T> t = GetTable<T>(tableName);

			t.PrimaryKey.Clear();
			t.PrimaryKey.AddRange(
				typeof(T).GetProperties()
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

		public async Task<T> FindAsync<T>(string tableName, T pk) => await FindAsync(GetTable<T>(tableName), pk);

		async Task<T> FindAsync<T>(Table<T> t, T pk)
		{
			if (t.PrimaryKey.Count <= 0)
				throw new Exception<T>(pbXNet.T.Localized("SDIM_PrimaryKeyNotDefined"), t, pk);

			return t.Rows.Find((o) =>
				t.PrimaryKey.Count == t.PrimaryKey.Count((p) => p.GetValue(pk).Equals(p.GetValue(o)))
			);
		}

		public async Task InsertAsync<T>(string tableName, T o) => await InsertAsync(GetTable<T>(tableName), o);

		async Task InsertAsync<T>(Table<T> t, T o)
		{
			T obj = await FindAsync(t, o);
			if (obj == null || obj.Equals(default(T)))
				t.Rows.Add(o);
			else
			{
				if (!object.ReferenceEquals(obj, o))
					t.Rows[t.Rows.IndexOf(obj)] = o;
			}
		}

		public async Task UpdateAsync<T>(string tableName, T o) => await UpdateAsync(GetTable<T>(tableName), o);

		async Task UpdateAsync<T>(Table<T> t, T o)
		{
			T obj = await FindAsync(t, o);
			if (obj == null || obj.Equals(default(T)))
				throw new Exception<T>(pbXNet.T.Localized("SDIM_ObjectDoesntExist"), t, o);

			if (!object.ReferenceEquals(obj, o))
				t.Rows[t.Rows.IndexOf(obj)] = o;
		}

		public async Task DeleteAsync<T>(string tableName, T pk) => await DeleteAsync(GetTable<T>(tableName), pk);

		async Task DeleteAsync<T>(Table<T> t, T pk)
		{
			T obj = await FindAsync(t, pk);
			if (obj == null || obj.Equals(default(T)))
				throw new Exception<T>(pbXNet.T.Localized("SDIM_ObjectDoesntExist"), t, pk);

			t.Rows.Remove(obj);
		}
	}
}
