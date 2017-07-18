using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public class SDCQuery<T> : IQuery<T> where T : new()
	{
		SqlBuilder Sql;

		const string ScalarExprPlaceholder = "_?";
		SqlBuilder ScalarSql;

		IDatabase _db;

		List<string> _dbWhere;
		List<Func<T, bool>> _localWhere;

		List<(bool expr, string exprOrColumnName)> _dbOrderBy;
		List<(bool desc, Delegate keySelector)> _localOrderBy;

		public SDCQuery(IDatabase db, string tableName)
		{
			Check.Null(db, nameof(db));
			Check.Empty(tableName, nameof(tableName));

			_db = db;

			Sql = _db.Sql.New();
			Sql.Select();
			foreach (var p in typeof(T).GetRuntimeProperties()) Sql.C(p.Name);
			Sql.From(tableName);

			ScalarSql = _db.Sql.New().Select().M(ScalarExprPlaceholder).From(tableName);
		}

		public SDCQuery(IDatabase db, SqlBuilder sql, SqlBuilder scalarSql = null)
		{
			Check.Null(db, nameof(db));
			Check.Null(sql, nameof(sql));

			_db = db;
			Sql = sql;
			ScalarSql = scalarSql;

			if (ScalarSql == null)
			{
				// TODO: try to build ScalarSql based on Sql
			}
		}

		public void Dispose()
		{
		}

		void CreateLocalWhere()
		{
			if (_localWhere == null)
				_localWhere = new List<Func<T, bool>>();
		}

		bool DbWhereDefined => _dbWhere != null && _dbWhere.Count > 0;
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

		void CreateLocalOrderBy()
		{
			if (_localOrderBy == null)
				_localOrderBy = new List<(bool, Delegate)>();
		}

		bool DbOrderByDefined => _dbOrderBy != null && _dbOrderBy.Count > 0;
		bool LocalOrderByDefined => _localOrderBy != null && _localOrderBy.Count > 0;

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

			if (DbOrderByDefined)
				throw new NotSupportedException("Using ORDER BY (SQL) in combination with OrderBy (Linq) is not supported."); // TODO: translation
			CreateLocalOrderBy();
			_localOrderBy.Add((false, expr.Compile()));

			return this;
		}

		public IQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> expr)
		{
			if (OrderByWithOneProperty<TKey>(expr, true))
				return this;

			// try translate expr to sql

			if (DbOrderByDefined)
				throw new NotSupportedException("Using ORDER BY DESC (SQL) in combination with OrderByDescending (Linq) is not supported."); // TODO: translation
			CreateLocalOrderBy();
			_localOrderBy.Add((true, expr.Compile()));

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
					if (ob.expr) sql.E(ob.exprOrColumnName);
					else sql.C(ob.exprOrColumnName);
				}
			}

			//string s = sql.Build();
			var q = await _db.QueryAsync<T>(sql.Build()).ConfigureAwait(false);

			if (LocalWhereDefined)
			{
				foreach (var predicate in _localWhere)
					q = q.Where(predicate);
			}

			if (LocalOrderByDefined)
			{
				foreach (var ob in _localOrderBy)
				{
					if(!ob.desc)
						q = q.OrderBy((Func<T, object>)ob.keySelector);
					else
						q = q.OrderByDescending((Func<T, object>)ob.keySelector);
				}
			}

			return q;
		}

		protected async Task<object> ScalarAsync(Func<IQueryResult<T>, object> localScalarFunc, string sqlScalarExpr)
		{
			if (ScalarSql == null || LocalWhereDefined)
			{
				using (var q = await PrepareAsync().ConfigureAwait(false))
					return localScalarFunc(q);
			}

			SqlBuilder sql = ScalarSql.Clone();

			if (DbWhereDefined)
			{
				// build db where
			}

			string _sql = sql.Build();
			_sql = _sql.Replace(ScalarExprPlaceholder, sqlScalarExpr);

			return await _db.ScalarAsync<object>(_sql).ConfigureAwait(false);
		}

		public async Task<bool> AnyAsync()
		{
			var rc = await ScalarAsync(q => q.Any(), Sql.Expr("1")).ConfigureAwait(false);
			return rc == null ? false : Convert.ToBoolean(rc);
		}

		public async Task<int> CountAsync()
		{
			var rc = await ScalarAsync(q => q.Count(), Sql.Expr("count(*)")).ConfigureAwait(false); // TODO: to SqlBuilder
			return rc == null ? 0 : Convert.ToInt32(rc);
		}
	}
}
