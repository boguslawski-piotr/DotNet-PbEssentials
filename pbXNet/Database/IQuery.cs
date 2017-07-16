using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet.Database
{
	public interface IQuery<T> : IDisposable, IEnumerable<T>
	{}

	public interface IQueryEx<T> : IQuery<T>
	{
		IQueryEx<T> Where(Expression<Func<T, bool>> expr);

		IQueryEx<T> OrderBy<K>(Expression<Func<T, K>> expr);

		IQueryEx<T> OrderByDescending<K>(Expression<Func<T, K>> expr);

		/// <summary>
		/// Should prepare and send a query to the database (based on data received from Where, OrderBy calls),
		/// perhaps retrieve some rows (for example using some cache strategy) and then prepare the enumerator for normal (in Linq manner) use.
		/// </summary>
		Task<IEnumerable<T>> PrepareAsync();

		Task<bool> AnyAsync();

		//CountAsync();
		//SelectAsync();
		//ToListAsync();
		//ToArrayAsync();

		// TODO: all linq extensions that can be optimized for execution in the database
	}
}
