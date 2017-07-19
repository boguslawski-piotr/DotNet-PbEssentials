using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public interface IQuery<T> : IDisposable where T : new()
	{
		// Should handle any number of calls combining expressions with the AND operator.
		IQuery<T> Where(Expression<Func<T, bool>> predicate);

		// First call should use OrderBy, next calls should use ThenBy (or when building SQL: ORDER BY (first), (second), ...).
		IQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);
		IQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);

		/// <summary>
		/// Should prepare and send a query to the database (based on data received from Where, OrderBy calls),
		/// perhaps retrieve some rows (for example using some cache strategy) and then prepare the enumerator for normal (in Linq manner) use.
		/// </summary>
		Task<IQueryResult<T>> QueryAsync();

		Task<bool> AnyAsync();

		Task<int> CountAsync();

		//SelectAsync();
		//ToListAsync();
		//ToArrayAsync();

		// TODO: all linq extensions that can be optimized for execution in the database
	}
}
