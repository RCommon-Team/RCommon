using RCommon.Models.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Queries
{
    /// <summary>
    /// Defines the contract for a query bus that dispatches queries to their corresponding handlers.
    /// </summary>
    /// <remarks>
    /// Queries represent requests for data that do not modify state. The bus resolves the appropriate
    /// <see cref="IQueryHandler{TQuery, TResult}"/> and returns the result.
    /// </remarks>
    public interface IQueryBus
    {
        /// <summary>
        /// Dispatches a query to its registered handler and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the query handler.</typeparam>
        /// <param name="query">The query to dispatch.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The result produced by the query handler.</returns>
        Task<TResult> DispatchQueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
    }
}
