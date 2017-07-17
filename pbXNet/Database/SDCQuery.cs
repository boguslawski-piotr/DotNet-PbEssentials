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

		const string ScalarFuncPlaceholder = "_?";
		SqlBuilder ScalarSql;

		IDatabase _db;

		List<(bool expr, string src)> _dbOrderBy;
		List<Func<T, bool>> _localWhere;

		public SDCQuery(IDatabase db, string tableName)
		{
			Check.Null(db, nameof(db));

			_db = db;

			Sql = _db.Sql.New();
			Sql.Select();
			foreach (var p in typeof(T).GetRuntimeProperties()) Sql.C(p.Name);
			Sql.From(tableName);

			ScalarSql = _db.Sql.New().Select().M(ScalarFuncPlaceholder).From(tableName);
		}

		public SDCQuery(IDatabase db, SqlBuilder sql)
		{
			Check.Null(db, nameof(db));

			_db = db;
			Sql = sql;
		}

		public void Dispose()
		{
		}

		void CreateLocalWhere()
		{
			if (_localWhere == null)
				_localWhere = new List<Func<T, bool>>();
		}

		bool DbWhereDefined => false;
		bool LocalWhereDefined => _localWhere != null && _localWhere.Count > 0;

		public IQuery<T> Where(Expression<Func<T, bool>> expr)
		{
			// TODO: try translate expr to sql

			CreateLocalWhere();
			_localWhere.Add(expr.Compile());

			return this;
		}

		void CreateDbOrderBy()
		{
			if (_dbOrderBy == null)
				_dbOrderBy = new List<(bool,string)>();
		}

		bool DbOrderByDefined => _dbOrderBy != null && _dbOrderBy.Count > 0;
		bool LocalOrderByDefined => false;

		bool OrderByWithOneProperty<TKey>(Expression<Func<T, TKey>> expr, bool desc)
		{
			PropertyInfo property = expr.AsPropertyInfo();
			if (property != null)
			{
				SqlBuilder sql = Sql.Expr().C(property.Name);
				if (desc) sql.Desc();

				CreateDbOrderBy();
				_dbOrderBy.Add((false, sql.Build()));

				return true;
			}

			return false;
		}

		public IQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> expr)
		{
			if (OrderByWithOneProperty<TKey>(expr, false))
				return this;

			// try translate expr to sql
			// if it not possible then add to local orderbys

			return this;
		}

		public IQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> expr)
		{
			if (OrderByWithOneProperty<TKey>(expr, true))
				return this;

			// try translate expr to sql
			// if it not possible then add to local orderbys

			return this;
		}

		public async Task<IQueryResult<T>> PrepareAsync()
		{
			SqlBuilder sql = Sql.Clone();

			if (DbWhereDefined)
			{
				// build db where
			}

			if (DbOrderByDefined)
			{
				sql.OrderBy();
				foreach (var ob in _dbOrderBy)
				{
					if (ob.expr) sql.E(ob.src);
					else sql.C(ob.src);
				}
			}

			//string s = sql.Build();
			var q = await _db.QueryAsync<T>(sql.Build());

			if (LocalWhereDefined)
			{
				foreach (var where in _localWhere)
					q.AddFilter(where);
			}

			if (LocalOrderByDefined)
			{
				// apply local order by
			}

			return q;
		}

		public async Task<bool> AnyAsync()
		{
			if (ScalarSql == null || LocalWhereDefined)
			{
				using (var q = await PrepareAsync())
					return q.Any();
			}

			SqlBuilder sql = ScalarSql.Clone();

			if (DbWhereDefined)
			{
				// build db where
			}

			string _sql = sql.Build();
			_sql = _sql.Replace(ScalarFuncPlaceholder, sql.Expr("1"));

			var rc = await _db.ScalarAsync<object>(_sql).ConfigureAwait(false);
			return rc != null;
		}
	}
}
