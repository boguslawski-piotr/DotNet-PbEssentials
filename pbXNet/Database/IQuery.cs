using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public interface IQueryResult<T> : IDisposable, IEnumerable<T> where T : new()
	{
		// should handle any number of calls combining expr with the AND operator
		IQueryResult<T> Where(Func<T, bool> predicate);

		// first call should use OrderBy, next calls should use ThenBy
		IQueryResult<T> OrderBy(Func<T, object> keySelector);
		IQueryResult<T> OrderByDescending(Func<T, object> keySelector);
	}

	public interface IQuery<T> : IDisposable where T : new()
	{
		// should handle any number of calls combining expr with the AND operator
		IQuery<T> Where(Expression<Func<T, bool>> expr);

		// first call should use OrderBy, next calls should use ThenBy (or in SQL: ORDER BY (first), (second), ...)
		IQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> expr);
		IQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> expr);

		/// <summary>
		/// Should prepare and send a query to the database (based on data received from Where, OrderBy calls),
		/// perhaps retrieve some rows (for example using some cache strategy) and then prepare the enumerator for normal (in Linq manner) use.
		/// </summary>
		Task<IQueryResult<T>> PrepareAsync();

		Task<bool> AnyAsync();
		Task<int> CountAsync();

		//SelectAsync();
		//ToListAsync();
		//ToArrayAsync();

		// TODO: all linq extensions that can be optimized for execution in the database
	}
}
