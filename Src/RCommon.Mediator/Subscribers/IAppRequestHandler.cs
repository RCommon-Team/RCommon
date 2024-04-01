using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.Subscribers
{
    public interface IAppRequestHandler<TRequest>
    {
        public Task HandleAsync(TRequest request, CancellationToken cancellationToken = default);
    }

    public interface IAppRequestHandler<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
    }
}
