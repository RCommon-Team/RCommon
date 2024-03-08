using MediatR;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR
{
    public class MediatRRequestHandler<TRequest, TResponse> : IRequestHandler<MediatRRequest<TRequest, TResponse>, TResponse>
    {
        private readonly IAppRequestHandler<TRequest, TResponse> myImpl;

        // injected by DI container
        public MediatRRequestHandler(IAppRequestHandler<TRequest, TResponse> impl)
        {
            myImpl = impl ?? throw new ArgumentNullException(nameof(impl));
        }

        public Task<TResponse> Handle(MediatRRequest<TRequest, TResponse> request, CancellationToken cancellationToken)
        {
            return myImpl.HandleAsync(request.Request, cancellationToken);
        }
    }
}
