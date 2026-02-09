using MediatR;

namespace RCommon.MediatR.Subscribers
{
    /// <summary>
    /// Non-generic marker interface for MediatR requests within the RCommon framework.
    /// Extends <see cref="IRequest"/> for fire-and-forget request semantics.
    /// </summary>
    public interface IMediatRRequest : IRequest
    {

    }

    /// <summary>
    /// Generic interface for MediatR requests that return a response.
    /// Extends both <see cref="IRequest{TResponse}"/> and <see cref="IMediatRRequest"/>.
    /// </summary>
    /// <typeparam name="TResponse">The response type returned by the request handler.</typeparam>
    public interface IMediatRRequest<TResponse> : IRequest<TResponse>, IMediatRRequest
    {

    }

    /// <summary>
    /// Generic interface for MediatR requests that wrap an underlying request payload and return a response.
    /// </summary>
    /// <typeparam name="TRequest">The type of the wrapped request payload.</typeparam>
    /// <typeparam name="TResponse">The response type returned by the request handler.</typeparam>
    public interface IMediatRRequest<TRequest, TResponse> : IMediatRRequest<TResponse>
    {
        /// <summary>
        /// Gets the underlying request payload.
        /// </summary>
        TRequest Request { get; }
    }
}