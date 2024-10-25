using RCommon.Models.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Queries
{
    public interface IQueryHandler
    {
    }

    public interface IQueryHandler<in TQuery, TResult> : IQueryHandler
        where TQuery : IQuery<TResult>
    {
        Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
    }
}
