using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using pbXNet;

namespace pbXNet.Database
{
	public class SDCQuery<T> : IQuery<T>
	{
		SqlBuilder Sql;
		SqlBuilder ScalarSql;

		IDatabase _db;
		QueryType _type;

		List<Func<T, bool>> _localWheres;

		public SDCQuery(IDatabase db, QueryType type, string source)
		{
			Check.Null(db, nameof(db));

			_db = db;
			_type = type;
			Sql = _db.Sql.New();

			switch (type)
			{
				case QueryType.Table:
					// TODO: all fields, not *
					Sql.Select().M("*").From(source);
					ScalarSql = _db.Sql.New().Select().E("?").From(source);
					break;

				case QueryType.Query:
					// TODO: handle QueryType.Query
					throw new NotImplementedException();
			}
		}

		public SDCQuery(IDatabase db, QueryType type, SqlBuilder sql)
		{
			Check.Null(db, nameof(db));

			_db = db;
			_type = type;
			Sql = sql;
		}

		public void Dispose()
		{
		}

		public IQuery<T> Where(Expression<Func<T, bool>> expr)
		{
			// TODO: try translate expr to sql

			if (_localWheres == null)
				_localWheres = new List<Func<T, bool>>();
			_localWheres.Add(expr.Compile());
			return this;
		}

		bool OrderByWithOneProperty<K>(Expression<Func<T, K>> expr)
		{
			PropertyInfo property = expr.AsPropertyInfo();
			if (property != null)
			{
				Sql.OrderBy().C(property.Name);
				return true;
			}

			return false;
		}

		public IQuery<T> OrderBy<K>(Expression<Func<T, K>> expr)
		{
			if (OrderByWithOneProperty<K>(expr))
				return this;

			// try translate expr to sql
			// if it not possible then add to local orderbys

			return this;
		}

		public IQuery<T> OrderByDescending<K>(Expression<Func<T, K>> expr)
		{
			if(OrderByWithOneProperty<K>(expr))
			{
				Sql.Desc();
				return this;
			}

			// try translate expr to sql
			// if it not possible then add to local orderbys

			return this;
		}

		public async Task<IQueryResult<T>> PrepareAsync()
		{
			// build db where

			// build db order by

			string s = Sql.Build();

			var q = await _db.QueryAsync<T>(Sql.Build());

			// apply local where
			foreach (var lw in _localWheres)
				q.AddLocalWhere(lw);

			// apply local order by

			return q;
		}

		public async Task<bool> AnyAsync()
		{
			if (_localWheres != null && _localWheres.Count > 0)
			{
				using (var q = await PrepareAsync())
					return q.Any();
			}

			// prepare scalar sql with defined db where

			throw new NotImplementedException();
		}
	}
}
