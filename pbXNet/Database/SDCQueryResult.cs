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
		DbDataReader _rows;
		DbCommand _cmd;
		IDatabase _db;

		List<Func<T, bool>> _localWheres;

		public SDCQueryResult(IDatabase db, DbCommand cmd, DbDataReader rows)
		{
			_rows = rows;
			_cmd = cmd;
			_db = db;
		}

		public void Dispose()
		{
			_rows?.Close();
			_rows?.Dispose();
			_rows = null;
			_cmd?.Dispose();
			_cmd = null;
			_db = null;
		}

		public void AddLocalWhere(Func<T, bool> where)
		{
			if (_localWheres == null)
				_localWheres = new List<Func<T, bool>>();

			_localWheres.Add(where);
		}

		public virtual IEnumerator GetEnumerator() => new Enumerator<T>(_db, _rows, _localWheres);
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => (IEnumerator<T>)this.GetEnumerator();

		public class Enumerator<T> : IEnumerator<T>
		{
			object IEnumerator.Current => Current;
			public T Current => _current;
			T _current = default(T);
			T _temp = default(T);

			IDictionary<string, PropertyInfo> _properties;

			IEnumerator _enumerator;
			IDatabase _db;

			List<Func<T, bool>> _localWheres;

			public Enumerator(IDatabase db, DbDataReader rows, List<Func<T, bool>> localWheres)
			{
				_db = db;
				_enumerator = rows.GetEnumerator();
				_localWheres = localWheres;

				Type t = typeof(T);
				if (t.IsClass)
				{
					_current = Activator.CreateInstance<T>();
					_temp = Activator.CreateInstance<T>();
				}

				_properties = t.GetRuntimeProperties().ToDictionary(_p => _p.Name);
			}

			void IDisposable.Dispose()
			{ }

			void PrepareCurrent(IDataRecord r, T v)
			{
				for (int i = 0; i < r.FieldCount; i++)
				{
					var p = _properties[r.GetName(i)];
					p.SetValue(v, _db.ConvertDbValueToPropertyValue(r.GetDataTypeName(i), r.IsDBNull(i) ? null : r.GetValue(i), p));
				}
			}

			public bool MoveNext()
			{
				if (_localWheres != null)
				{
					while (_enumerator.MoveNext())
					{
						PrepareCurrent((DbDataRecord)_enumerator.Current, _temp);

						bool ok = false;
						foreach (var where in _localWheres)
						{
							ok = where(_temp);
							if (!ok)
								break;
						}

						if (ok)
						{
							PrepareCurrent((DbDataRecord)_enumerator.Current, _current);
							return true;
						}
					}
				}
				else
				{
					if (_enumerator.MoveNext())
					{
						PrepareCurrent((DbDataRecord)_enumerator.Current, _current);
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
