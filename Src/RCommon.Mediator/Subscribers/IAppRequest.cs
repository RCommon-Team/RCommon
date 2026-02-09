using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.Subscribers
{
    /// <summary>
    /// Marker interface for request messages that are dispatched to a single handler with no return value.
    /// </summary>
    /// <remarks>
    /// Implement this interface on command or request DTOs that should be handled by exactly one
    /// <see cref="IAppRequestHandler{TRequest}"/> and do not produce a response.
    /// </remarks>
    public interface IAppRequest
    {

    }

    /// <summary>
    /// Marker interface for request messages that are dispatched to a single handler and return a response.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response produced by the handler.</typeparam>
    /// <remarks>
    /// Implement this interface on query or request DTOs that should be handled by exactly one
    /// <see cref="IAppRequestHandler{TRequest, TResponse}"/> and produce a result of type <typeparamref name="TResponse"/>.
    /// </remarks>
    public interface IAppRequest<out TResponse>
    {

    }
}
