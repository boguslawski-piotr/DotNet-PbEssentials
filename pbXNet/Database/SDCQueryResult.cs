using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public class SDCQueryResult<T> : IQueryResult<T>
	{
		IDatabase _db;
		DbCommand _cmd;
		DbDataReader _rows;
		List<Func<T, bool>> _filters;

		public SDCQueryResult(IDatabase db, DbCommand cmd, DbDataReader rows)
		{
			Check.Null(db, nameof(db));
			Check.Null(cmd, nameof(cmd));
			Check.Null(rows, nameof(rows));

			_rows = rows;
			_cmd = cmd;
			_db = db;
		}

		public void Dispose()
		{
			_filters?.Clear();
			_filters = null;
			_rows?.Close();
			_rows?.Dispose();
			_rows = null;
			_cmd?.Dispose();
			_cmd = null;
			_db = null;
		}

		public void AddFilter(Func<T, bool> where)
		{
			if (_filters == null)
				_filters = new List<Func<T, bool>>();

			_filters.Add(where);
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public virtual IEnumerator<T> GetEnumerator() => new Enumerator<T>(this); 

		public class Enumerator<T> : IEnumerator<T>
		{
			object IEnumerator.Current => Current;
			public T Current => _current;

			T _current = default(T);
			T _temp = default(T);

			IDictionary<string, PropertyInfo> _properties;
			SDCQueryResult<T> _queryResult;
			IEnumerator _enumerator;

			public Enumerator(SDCQueryResult<T> queryResult)
			{
				Check.Null(queryResult, nameof(queryResult));

				Type t = typeof(T);
				if (t.IsClass)
				{
					_current = Activator.CreateInstance<T>();
					_temp = Activator.CreateInstance<T>();
				}

				_properties = t.GetRuntimeProperties().ToDictionary(_p => _p.Name);

				_queryResult = queryResult;
				_enumerator = _queryResult._rows.GetEnumerator();
			}

			void IDisposable.Dispose()
			{
				_properties?.Clear();
				_properties = null;
				_queryResult = null;
				_enumerator = null;
				_current = _temp = default(T);
			}

			void GetTValue(IDataRecord r, ref T v)
			{
				for (int i = 0; i < r.FieldCount; i++)
				{
					var p = _properties[r.GetName(i)];
					p.SetValue(v, _queryResult._db.ConvertDbValueToPropertyValue(r.GetDataTypeName(i), r.IsDBNull(i) ? null : r.GetValue(i), p));
				}
			}

			public bool MoveNext()
			{
				if (_queryResult._filters != null)
				{
					while (_enumerator.MoveNext())
					{
						GetTValue((DbDataRecord)_enumerator.Current, ref _temp);

						bool ok = false;
						foreach (var where in _queryResult._filters)
						{
							ok = where(_temp);
							if (!ok)
								break;
						}

						if (ok)
						{
							GetTValue((DbDataRecord)_enumerator.Current, ref _current);
							return true;
						}
					}
				}
				else
				{
					if (_enumerator.MoveNext())
					{
						GetTValue((DbDataRecord)_enumerator.Current, ref _current);
						return true;
					}
				}

				_current = _temp = default(T);
				return false;
			}

			public void Reset()
			{
				_current = default(T);
				_enumerator.Reset();
			}
		}
	}
}
