using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// This class is not thread safe. The behavior of the same instance 
	/// used in different threads is undefined and can generate random data 
	/// or references to undefined data.
	/// </remarks>
	public class SqlQuery<T> : IQuery<T> where T : new()
	{
		protected static readonly string _scalarExprPlaceholder = "_?";

		protected readonly IDatabase _db;

		protected readonly SqlBuilder _sqlBuilder;

		protected readonly SqlBuilder _scalarSqlBuilder;

		protected IExpressionTranslator _expressionTranslator;

		protected List<string> _dbWhere;

		protected List<Func<T, bool>> _localWhere;

		protected List<(bool desc, string expr)> _dbOrderBy;

		protected List<(bool desc, Delegate keySelector)> _localOrderBy;

		public SqlQuery(IDatabase db, string tableName)
		{
			Check.Null(db, nameof(db));
			Check.Empty(tableName, nameof(tableName));

			_db = db;

			_sqlBuilder = _db.GetSqlBuilder();

			_sqlBuilder.Select();
			foreach (var c in typeof(T).GetRuntimePropertiesAndFields())
				_sqlBuilder.C(c.Name);
			_sqlBuilder.From(tableName);

			_scalarSqlBuilder = _db.GetSqlBuilder().Select().Text(_scalarExprPlaceholder).From(tableName);
		}

		public SqlQuery(IDatabase db, string sql, string scalarSql)
		{
			Check.Null(db, nameof(db));
			Check.Empty(sql, nameof(sql));

			_db = db;

			_sqlBuilder = _db.GetSqlBuilder().Start(sql);

			_scalarSqlBuilder = string.IsNullOrWhiteSpace(scalarSql) ? null : _db.GetSqlBuilder().Start(scalarSql);

			if (_scalarSqlBuilder == null)
			{
				_scalarSqlBuilder = _sqlBuilder.Clone();

				if (!_scalarSqlBuilder.ReplaceSelectColumnsWith(_scalarExprPlaceholder))
					_scalarSqlBuilder = null;
			}
		}

		public virtual void Dispose()
		{
			_dbWhere?.Clear();
			_localWhere?.Clear();
			_dbOrderBy?.Clear();
			_localOrderBy?.Clear();
		}

		protected virtual string TranslateExpression(Expression expr, [CallerMemberName]string callerName = null)
		{
			if (_expressionTranslator == null)
				_expressionTranslator = _db.GetExpressionTranslator(typeof(T));

			string sexpr = _expressionTranslator.Translate(expr);

			if (sexpr != null)
				Log.D($"translated: '{expr.ToString()}' to expression: '{sexpr}'.", this, callerName);
			else
				Log.D($"translation failed for '{expr.ToString()}'. This filter will be applied locally.", this, callerName);

			return sexpr;
		}

		protected void CreateDbWhere()
		{
			if (_dbWhere == null)
				_dbWhere = new List<string>();
		}

		protected bool DbWhereDefined => _dbWhere != null && _dbWhere.Count > 0;

		protected void CreateLocalWhere()
		{
			if (_localWhere == null)
				_localWhere = new List<Func<T, bool>>();
		}

		protected bool LocalWhereDefined => _localWhere != null && _localWhere.Count > 0;

		public virtual IQuery<T> Where(Expression<Func<T, bool>> predicate)
		{
			Check.Null(predicate, nameof(predicate));

			string sqlExpr = TranslateExpression(predicate.Body);
			if (sqlExpr != null)
			{
				CreateDbWhere();
				_dbWhere.Add(sqlExpr);

				return this;
			}

			CreateLocalWhere();
			_localWhere.Add(predicate.Compile());

			return this;
		}

		protected void CreateDbOrderBy()
		{
			if (_dbOrderBy == null)
				_dbOrderBy = new List<(bool, string)>();
		}

		protected bool DbOrderByDefined => _dbOrderBy != null && _dbOrderBy.Count > 0;

		protected void CreateLocalOrderBy()
		{
			if (_localOrderBy == null)
				_localOrderBy = new List<(bool, Delegate)>();
		}

		protected bool LocalOrderByDefined => _localOrderBy != null && _localOrderBy.Count > 0;

		protected virtual bool OrderBy<TKey>(Expression<Func<T, TKey>> keySelector, bool desc)
		{
			Check.Null(keySelector, nameof(keySelector));

			string sqlExpr = TranslateExpression(keySelector.Body);
			if (sqlExpr != null)
			{
				CreateDbOrderBy();
				_dbOrderBy.Add((desc, sqlExpr));

				return true;
			}

			return false;
		}

		public virtual IQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
		{
			if (OrderBy<TKey>(keySelector, false))
				return this;

			if (DbOrderByDefined)
				throw new NotSupportedException("Using ORDER BY (SQL) in combination with OrderBy (Linq) is not supported."); // TODO: translation

			CreateLocalOrderBy();
			_localOrderBy.Add((false, keySelector.Compile()));

			return this;
		}

		public virtual IQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
		{
			if (OrderBy<TKey>(keySelector, true))
				return this;

			if (DbOrderByDefined)
				throw new NotSupportedException("Using ORDER BY DESC (SQL) in combination with OrderByDescending (Linq) is not supported."); // TODO: translation

			CreateLocalOrderBy();
			_localOrderBy.Add((true, keySelector.Compile()));

			return this;
		}

		protected virtual bool BuildDbWhere(SqlBuilder sqlBuilder)
		{
			Check.Null(sqlBuilder, nameof(sqlBuilder));

			if (DbWhereDefined)
			{
				sqlBuilder.Where();
				for (int n = 0; n < _dbWhere.Count; n++)
				{
					if (n > 0) sqlBuilder.And();
					sqlBuilder.Ob().Text(_dbWhere[n]).Cb();
				}

				return true;
			}

			return false;
		}

		public virtual async Task<IQueryResult<T>> ResultAsync()
		{
			SqlBuilder sqlBuilder = _sqlBuilder.Clone();

			bool useTranslatorParameters = BuildDbWhere(sqlBuilder);

			if (DbOrderByDefined)
			{
				useTranslatorParameters = true;

				sqlBuilder.OrderBy();
				foreach (var ob in _dbOrderBy)
				{
					sqlBuilder.E(ob.expr);
					if (ob.desc) sqlBuilder.Desc();
				}
			}

			//string s = sqlBuilder.Build();
			var q = await _db.QueryAsync<T>(sqlBuilder.Build(), useTranslatorParameters ? _expressionTranslator.Parameters.ToArray() : null).ConfigureAwait(false);

			if (LocalWhereDefined)
			{
				foreach (var predicate in _localWhere)
					q = q.Where(predicate);
			}

			if (LocalOrderByDefined)
			{
				foreach (var ob in _localOrderBy)
				{
					if (!ob.desc)
						q = q.OrderBy((Func<T, object>)ob.keySelector);
					else
						q = q.OrderByDescending((Func<T, object>)ob.keySelector);
				}
			}

			return q;
		}

		protected virtual async Task<object> ScalarAsync(Func<IQueryResult<T>, object> localScalarFunc, string sqlScalarExpr)
		{
			Check.Null(localScalarFunc, nameof(localScalarFunc));
			Check.Null(sqlScalarExpr, nameof(sqlScalarExpr));

			if (_scalarSqlBuilder == null || LocalWhereDefined)
			{
				using (var q = await ResultAsync().ConfigureAwait(false))
					return localScalarFunc(q);
			}

			SqlBuilder sqlBuilder = _scalarSqlBuilder.Clone();

			bool useTranslatorParameters = BuildDbWhere(sqlBuilder);

			string sql = sqlBuilder.Build();
			sql = sql.Replace(_scalarExprPlaceholder, sqlScalarExpr);

			return await _db.ScalarAsync<object>(sql, useTranslatorParameters ? _expressionTranslator.Parameters.ToArray() : null).ConfigureAwait(false);
		}

		public async Task<bool> AnyAsync()
		{
			var result = await ScalarAsync(q => q.Any(), _sqlBuilder.New().Expr("1")).ConfigureAwait(false);
			return result == null ? false : Convert.ToBoolean(result);
		}

		public async Task<int> CountAsync()
		{
			var result = await ScalarAsync(q => q.Count(), _sqlBuilder.New().Count()).ConfigureAwait(false);
			return result == null ? 0 : Convert.ToInt32(result);
		}
	}
}
