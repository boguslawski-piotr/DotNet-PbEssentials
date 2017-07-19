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

		protected IDictionary<string, PropertyInfo> _properties;

		protected List<Func<T, bool>> _filters;
		protected IOrderedEnumerable<T> _orderedRows;

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
			_orderedRows = null;
			_filters?.Clear();
			_filters = null;
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
					p.SetValue(v, _db.ConvertDbValueToPropertyValue(r.GetDataTypeName(i), r.IsDBNull(i) ? null : r.GetValue(i), p));
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
				while (_rows.Read())
				{
					T v = new T();
					GetRowData(_rows, ref v);

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

					if (ok)
					{
						yield return v;
					}
				}
			}
		}
	}
}
