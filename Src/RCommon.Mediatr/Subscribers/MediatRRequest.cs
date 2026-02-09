using MediatR;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR.Subscribers
{
    /// <summary>
    /// Wrapper class that adapts a request of type <typeparamref name="TRequest"/> into an
    /// <see cref="IMediatRRequest{TRequest}"/> for fire-and-forget processing through the MediatR pipeline.
    /// </summary>
    /// <typeparam name="TRequest">The type of the wrapped request payload.</typeparam>
    public class MediatRRequest<TRequest> : IMediatRRequest<TRequest>
    {

        /// <summary>
        /// Initializes a new instance of <see cref="MediatRRequest{TRequest}"/> with the specified request payload.
        /// </summary>
        /// <param name="request">The request payload to wrap.</param>
        public MediatRRequest(TRequest request)
        {
            Request = request;
        }

        /// <inheritdoc />
        public TRequest Request { get; }
    }

    /// <summary>
    /// Wrapper class that adapts a request of type <typeparamref name="TRequest"/> into an
    /// <see cref="IMediatRRequest{TRequest, TResponse}"/> for request/response processing through the MediatR pipeline.
    /// </summary>
    /// <typeparam name="TRequest">The type of the wrapped request payload.</typeparam>
    /// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
    public class MediatRRequest<TRequest, TResponse> : IMediatRRequest<TRequest, TResponse>
    {

        /// <summary>
        /// Initializes a new instance of <see cref="MediatRRequest{TRequest, TResponse}"/> with the specified request payload.
        /// </summary>
        /// <param name="request">The request payload to wrap.</param>
        public MediatRRequest(TRequest request)
        {
            Request = request;
        }

        /// <inheritdoc />
        public TRequest Request { get; }
    }
}
