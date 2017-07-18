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
		SDCQueryResult<T> _parent;

		IDatabase _db;
		DbCommand _cmd;
		DbDataReader _rows;
		IDictionary<string, PropertyInfo> _properties;

		List<Func<T, bool>> _filters;
		IOrderedEnumerable<T> _orderedRows;

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

		public void Dispose()
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

		public IQueryResult<T> Where(Func<T, bool> predicate)
		{
			if (_filters == null)
				_filters = new List<Func<T, bool>>();

			_filters.Add(predicate);

			return this;
		}

		public IQueryResult<T> OrderBy(Func<T, object> keySelector)
		{
			return new SDCQueryResult<T>(this, _orderedRows == null ? Enumerable.OrderBy(this, keySelector) : _orderedRows.ThenBy(keySelector));
		}

		public IQueryResult<T> OrderByDescending(Func<T, object> keySelector)
		{
			return new SDCQueryResult<T>(this, _orderedRows == null ? Enumerable.OrderByDescending(this, keySelector) : _orderedRows.ThenByDescending(keySelector));
		}

		void GetRowData(IDataRecord r, ref T v)
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

		//public virtual IEnumerator<T> GetEnumerator() => new Enumerator<T>(this);

		//public class Enumerator<T> : IEnumerator<T> where T : new()
		//{
		//	object IEnumerator.Current => Current;
		//	public T Current => _current;

		//	IDictionary<string, PropertyInfo> _properties;
		//	SDCQueryResult<T> _queryResult;
		//	IEnumerator _enumerator;
		//	T _current;

		//	public Enumerator(SDCQueryResult<T> queryResult)
		//	{
		//		Check.Null(queryResult, nameof(queryResult));

		//		_properties = typeof(T).GetRuntimeProperties().ToDictionary(_p => _p.Name);
		//		_queryResult = queryResult;
		//		_enumerator = _queryResult._rows.GetEnumerator();
		//		_current = new T();
		//	}

		//	void IDisposable.Dispose()
		//	{
		//		_properties?.Clear();
		//		_properties = null;
		//		_queryResult = null;
		//		_enumerator = null;
		//		_current = default(T);
		//	}

		//	void GetTValue(IDataRecord r, ref T v)
		//	{
		//		for (int i = 0; i < r.FieldCount; i++)
		//		{
		//			if (_properties.TryGetValue(r.GetName(i), out PropertyInfo p))
		//				p.SetValue(v, _queryResult._db.ConvertDbValueToPropertyValue(r.GetDataTypeName(i), r.IsDBNull(i) ? null : r.GetValue(i), p));
		//		}
		//	}

		//	public bool MoveNext()
		//	{
		//		if (_queryResult._filters != null)
		//		{
		//			while (_enumerator.MoveNext())
		//			{
		//				T _temp = new T();
		//				GetTValue((DbDataRecord)_enumerator.Current, ref _temp);

		//				bool ok = false;
		//				foreach (var predicate in _queryResult._filters)
		//				{
		//					ok = predicate(_temp);
		//					if (!ok)
		//						break;
		//				}

		//				if (ok)
		//				{
		//					_current = _temp;
		//					return true;
		//				}
		//			}
		//		}
		//		else
		//		{
		//			if (_enumerator.MoveNext())
		//			{
		//				GetTValue((DbDataRecord)_enumerator.Current, ref _current);
		//				return true;
		//			}
		//		}

		//		_current = default(T);
		//		return false;
		//	}

		//	public void Reset()
		//	{
		//		_current = default(T);
		//		_enumerator.Reset();
		//	}
		//}
	}
}
