using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Queries
{
    public interface IQueryBus
    {
        Task<TResult> DispatchQueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
    }
}
