using System;
using System.Collections.Generic;
using System.Text;

namespace pbXNet.Database
{
	public interface IQueryResult<T> : IDisposable, IEnumerable<T> where T : new()
	{
		// Should handle any number of calls combining predicates with the AND operator.
		IQueryResult<T> Where(Func<T, bool> predicate);

		// First call should use OrderBy, next calls should use ThenBy.
		IQueryResult<T> OrderBy(Func<T, object> keySelector);
		IQueryResult<T> OrderByDescending(Func<T, object> keySelector);
	}
}
