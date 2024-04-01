using MediatR;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Mediator.Subscribers;
using IAppRequest = RCommon.Mediator.Subscribers.IAppRequest;

namespace RCommon.MediatR.Subscribers
{
    public class MediatRRequestHandler<T, TRequest> : IRequestHandler<TRequest>
        where T : class, IAppRequest
        where TRequest : MediatRRequest<T>
    {
        private readonly IServiceProvider _serviceProvider;

        public MediatRRequestHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Handle(TRequest request, CancellationToken cancellationToken)
        {
            // Resolve the actual event handler that we want to execute
            var handler = (IAppRequestHandler<T>)_serviceProvider.GetService(typeof(IAppRequestHandler<T>));

            Guard.Against<NullReferenceException>(handler == null,
                "IAppRequestHandler<T> of type: " + typeof(T).GetGenericTypeName() + " could not be resolved by IServiceProvider");

            // Handle the event using the event handler we resolved
            await handler.HandleAsync(request.Request);
        }
    }
}
