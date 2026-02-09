using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator
{
    /// <summary>
    /// Default implementation of <see cref="IMediatorService"/> that delegates all operations to an <see cref="IMediatorAdapter"/>.
    /// </summary>
    /// <remarks>
    /// This service acts as a thin wrapper around <see cref="IMediatorAdapter"/>, providing a consistent
    /// application-facing API while the adapter handles library-specific dispatch logic.
    /// Registered as a scoped service by <see cref="MediatorBuilderExtensions.WithMediator{T}(IRCommonBuilder, Action{T})"/>.
    /// </remarks>
    public class MediatorService : IMediatorService
    {
        private readonly IMediatorAdapter _mediatorAdapter;

        /// <summary>
        /// Initializes a new instance of <see cref="MediatorService"/>.
        /// </summary>
        /// <param name="mediatorAdapter">The mediator adapter that handles the actual dispatch to handlers.</param>
        public MediatorService(IMediatorAdapter mediatorAdapter)
        {
            _mediatorAdapter = mediatorAdapter;
        }

        /// <inheritdoc />
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        {
            return _mediatorAdapter.Publish(notification, cancellationToken);
        }

        /// <inheritdoc />
        public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            await _mediatorAdapter.Send(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        {
            return await _mediatorAdapter.Send<TRequest, TResponse>(request, cancellationToken);
        }

    }
}
