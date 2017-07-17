using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public class SDCTable<T> : ITable<T>
	{
		public string Name { get; private set; }

		List<PropertyInfo> _properties;
		List<PropertyInfo> _primaryKey;

		SqlBuilder Sql;
		IDatabase _db;

		protected SDCTable(IDatabase db, string name)
		{
			Sql = db.Sql.New();
			_db = db;

			Name = name;
			_properties = typeof(T).GetRuntimeProperties()
				.ToList();
			_primaryKey = _properties
				.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == nameof(PrimaryKeyAttribute)))
				.ToList();
		}

		public static async Task<ITable<T>> OpenAsync(IDatabase db, string name)
		{
			Check.Null(db, nameof(db));
			Check.Empty(name, nameof(name));

			SDCTable<T> table = new SDCTable<T>(db, name);

			await table.OpenAsync();

			return table;
		}

		public async Task OpenAsync()
		{
			// TODO: check if table has correct structure (as T definition), permorm upgrade/downgrade if needed
		}

		public static async Task<ITable<T>> CreateAsync(IDatabase db, string name)
		{
			Check.Null(db, nameof(db));
			Check.Empty(name, nameof(name));

			SDCTable<T> table = new SDCTable<T>(db, name);

			await table.CreateAsync();

			return table;
		}

		public async Task CreateAsync()
		{
			Sql.Create().Table(Name);

			foreach (var p in _properties)
			{
				Sql.C(p.Name);

				var attrs = p.CustomAttributes.ToList();

				bool notNull =
					_primaryKey.Contains(p) ||
					attrs.Find(a => a.AttributeType.Name == nameof(NotNullAttribute)) != null;

				_db.ConvertPropertyTypeToDbType(p, Sql);

				if (notNull)
					Sql.NotNull();
				else
					Sql.Null();
			}

			if (_primaryKey.Count > 0)
			{
				Sql.Constraint($"PK_{Name}").PrimaryKey();
				foreach (var p in _primaryKey)
					Sql.C(p.Name);
			}

			await _db.StatementAsync(Sql.Build()).ConfigureAwait(false);

			await CreateIndexesAsync();
		}

		public async Task CreateIndexesAsync()
		{
			IDictionary<string, List<(string name, bool desc)>> indexes = new Dictionary<string, List<(string, bool)>>();

			foreach (var p in _properties)
			{
				CustomAttributeData indexAttr = p.CustomAttributes.ToList().Find(a => a.AttributeType.Name == nameof(IndexAttribute));
				if (indexAttr != null)
				{
					string indexName = (string)indexAttr.ConstructorArguments[0].Value;

					if (!indexes.TryGetValue(indexName, out List<(string, bool)> indexColumns))
						indexColumns = new List<(string, bool)>();
					indexColumns.Add((p.Name, (bool)indexAttr.ConstructorArguments[1].Value));

					indexes[indexName] = indexColumns;
				}
			}

			foreach (var index in indexes)
			{
				string indexName = $"IX_{Name}_{index.Key}";

				Sql.Drop().Index(indexName).On(Name);
				await _db.StatementAsync(Sql.Build()).ConfigureAwait(false);

				Sql.Create().Index(indexName).On(Name);
				foreach (var indexColumn in index.Value)
				{
					Sql.C(indexColumn.name);
					if (indexColumn.desc) Sql.Desc();
				}
				await _db.StatementAsync(Sql.Build()).ConfigureAwait(false);
			}
		}

		public void Dispose() {}

		public IQuery<T> Rows => new SDCQuery<T>(_db, QueryType.Table, Name);

		protected void BuildWhereForPrimaryKey(T pk, List<object> parametres)
		{
			if (_primaryKey.Count <= 0)
				throw new Exception(pbXNet.T.Localized("DB_PrimaryKeyNotDefined", Name));

			Sql.Where();

			int n = 1;
			int pc = parametres.Count;
			foreach (var p in _primaryKey)
			{
				if (n > 1) Sql.And();
				Sql.C(p.Name).Eq.P(pc + n++);

				object v = p.GetValue(pk);
				if (v == null)
					throw new Exception($"The property '{p.Name}' that is part of the primary key can not be null."); // TODO: translation

				parametres.Add(_db.ConvertPropertyValueToDbValue(v, p));
			}
		}

		public async Task<bool> ExistsAsync(T pk)
		{
			Check.Null(pk, nameof(pk));

			var parametres = new List<object>();

			Sql.Select().E("1").From(Name);
			BuildWhereForPrimaryKey(pk, parametres);

			return await _db.ScalarAsync<object>(Sql.Build(), parametres.ToArray()).ConfigureAwait(false) != null;
		}

		public async Task<T> FindAsync(T pk)
		{
			Check.Null(pk, nameof(pk));

			Sql.Select();
			foreach (var p in _properties) Sql.C(p.Name);
			Sql.From(Name);

			var parametres = new List<object>();
			BuildWhereForPrimaryKey(pk, parametres);

			using (IQueryResult<T> r = await _db.QueryAsync<T>(Sql.Build(), parametres.ToArray()))
			{
				IEnumerator<T> e = r.GetEnumerator();
				return e.MoveNext() ? e.Current : default(T);
			}
		}

		public async Task UpdateAsync(T o)
		{
			Check.Null(o, nameof(o));

			var parametres = new List<object>();

			Sql.Update(Name);
			for (int n = 0; n < _properties.Count; n++)
			{
				var p = _properties[n];
				Sql.C(p.Name).P(n + 1);
				parametres.Add(_db.ConvertPropertyValueToDbValue(p.GetValue(o), p));
			}

			BuildWhereForPrimaryKey(o, parametres);

			if (await _db.StatementAsync(Sql.Build(), parametres.ToArray()) <= 0)
				throw new Exception("Record not found!"); // TODO: localization and much better text!
		}

		public async Task InsertOrUpdateAsync(T o)
		{
			Check.Null(o, nameof(o));

			if (_primaryKey.Count <= 0 || !await ExistsAsync(o))
			{
				var parametres = new List<object>();

				Sql.InsertInto(Name);
				foreach (var p in _properties)
				{
					Sql.C(p.Name);
					parametres.Add(_db.ConvertPropertyValueToDbValue(p.GetValue(o), p));
				}

				Sql.Values();
				for (int n = 1; n <= _properties.Count; n++)
					Sql.P(n);

				await _db.StatementAsync(Sql.Build(), parametres.ToArray());
			}
			else
				await UpdateAsync(o);
		}

		public async Task DeleteAsync(T o)
		{
			Check.Null(o, nameof(o));

			if (_primaryKey.Count <= 0 || !await ExistsAsync(o))
			{
				// TODO: delete all rows that match T o
			}
			else
			{
				var parametres = new List<object>();

				Sql.Delete().From(Name);

				BuildWhereForPrimaryKey(o, parametres);

				await _db.StatementAsync(Sql.Build(), parametres.ToArray());
			}
		}
	}
}

/* CreateIndex:

foreach (var c in columns)
{
	PropertyInfo p = c.AsPropertyInfo<T>();
	if (p == null)
		throw new ArgumentException(pbXNet.T.Localized("DB_NotPropertyExpression", Type.FullName), c.ToString());
	_primaryKey.Add(p);
}

*/
