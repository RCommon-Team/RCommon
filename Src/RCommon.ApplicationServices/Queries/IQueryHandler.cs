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
    /// Non-generic marker interface for all query handlers. Used for service resolution via reflection.
    /// </summary>
    public interface IQueryHandler
    {
    }

    /// <summary>
    /// Defines the contract for a handler that processes a specific query type and returns a result.
    /// </summary>
    /// <typeparam name="TQuery">The query type to handle.</typeparam>
    /// <typeparam name="TResult">The type of result produced by handling the query.</typeparam>
    public interface IQueryHandler<in TQuery, TResult> : IQueryHandler
        where TQuery : IQuery<TResult>
    {
        /// <summary>
        /// Handles the specified query asynchronously and returns the result.
        /// </summary>
        /// <param name="query">The query to handle.</param>
        /// <param name="cancellationToken">Token to observe for cancellation.</param>
        /// <returns>The result produced by handling the query.</returns>
        Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
    }
}
