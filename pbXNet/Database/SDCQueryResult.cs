using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace pbXNet.Database
{
	public class SDCQueryResult<T> : IQueryResult<T> where T : new()
	{
		protected SDCQueryResult<T> _parent;

		protected IDatabase _db;

		protected DbCommand _cmd;

		protected DbDataReader _rows;

		protected IOrderedEnumerable<T> _orderedRows;

		protected Lazy<List<T>> _cachedRows = new Lazy<List<T>>(() => new List<T>(), true);

		protected volatile bool _useCachedRows;

		protected List<Func<T, bool>> _filters;

		protected IDictionary<string, PropertyInfo> _properties;

		public SDCQueryResult(IDatabase db, DbCommand cmd, DbDataReader rows)
		{
			Check.Null(db, nameof(db));
			Check.Null(rows, nameof(rows));

			_rows = rows;
			_cmd = cmd;
			_db = db;

			_properties = typeof(T).GetRuntimeProperties().ToDictionary(_p => _p.Name);
		}

		public SDCQueryResult(SDCQueryResult<T> parent, IOrderedEnumerable<T> orderedRows)
		{
			_parent = parent;
			_orderedRows = orderedRows;
		}

		public virtual void Dispose()
		{
			_parent?.Dispose();
			_parent = null;
			_properties?.Clear();
			_properties = null;
			_filters?.Clear();
			_filters = null;
			_orderedRows = null;
			_cachedRows?.Value?.Clear();
			_cachedRows = null;
			_rows?.Close();
			_rows?.Dispose();
			_rows = null;
			_cmd?.Dispose();
			_cmd = null;
			_db = null;
		}

		public virtual IQueryResult<T> Where(Func<T, bool> predicate)
		{
			if (_filters == null)
				_filters = new List<Func<T, bool>>();

			_filters.Add(predicate);

			return this;
		}

		protected virtual bool ApplyFilters(T v)
		{
			bool ok = _filters == null;
			if (!ok)
			{
				foreach (var predicate in _filters)
				{
					ok = predicate(v);
					if (!ok)
						break;
				}
			}

			return ok;
		}

		public virtual IQueryResult<T> OrderBy(Func<T, object> keySelector)
		{
			return new SDCQueryResult<T>(this, _orderedRows == null ? Enumerable.OrderBy(this, keySelector) : _orderedRows.ThenBy(keySelector));
		}

		public virtual IQueryResult<T> OrderByDescending(Func<T, object> keySelector)
		{
			return new SDCQueryResult<T>(this, _orderedRows == null ? Enumerable.OrderByDescending(this, keySelector) : _orderedRows.ThenByDescending(keySelector));
		}

		protected virtual void GetRowData(IDataRecord r, ref T v)
		{
			for (int i = 0; i < r.FieldCount; i++)
			{
				if (_properties.TryGetValue(r.GetName(i), out PropertyInfo p))
					p.SetValue(v, _db.ConvertDbValueToValue(r.GetDataTypeName(i), r.IsDBNull(i) ? null : r.GetValue(i), p.PropertyType));
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		public virtual IEnumerator<T> GetEnumerator()
		{
			if (_orderedRows != null)
			{
				var e = _orderedRows.GetEnumerator();
				while (e.MoveNext())
					yield return e.Current;
			}
			else
			{
				if (_useCachedRows)
				{
					foreach (var v in _cachedRows.Value)
					{
						if (ApplyFilters(v))
							yield return v;
					}
				}
				else
				{
					while (_rows.Read())
					{
						T v = new T();
						GetRowData(_rows, ref v);

						if (ApplyFilters(v))
						{
							_cachedRows.Value.Add(v);
							yield return v;
						}
					}

					_useCachedRows = true;
				}
			}
		}
	}
}
