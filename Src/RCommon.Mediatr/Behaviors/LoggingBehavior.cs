using MediatR;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR.Behaviors
{
    /// <summary>
    /// MediatR pipeline behavior that logs the handling of fire-and-forget requests (no explicit response type).
    /// Logs the command name and payload before and after the handler executes.
    /// </summary>
    /// <typeparam name="TRequest">The MediatR request type. Must implement <see cref="IRequest"/>.</typeparam>
    /// <typeparam name="TResponse">The response type from the pipeline.</typeparam>
    public class LoggingRequestBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest
    {
        private readonly ILogger<LoggingRequestBehavior<TRequest, TResponse>> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="LoggingRequestBehavior{TRequest, TResponse}"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        public LoggingRequestBehavior(ILogger<LoggingRequestBehavior<TRequest, TResponse>> logger) => _logger = logger;


        /// <inheritdoc />
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _logger.LogInformation("----- Handling command {CommandName} ({@Command})", request.GetGenericTypeName(), request);
            var response = await next();
            _logger.LogInformation("----- Command {CommandName} handled - response: {@Response}", request.GetGenericTypeName(), response);

            return response;
        }
    }

    /// <summary>
    /// MediatR pipeline behavior that logs the handling of requests that return a response.
    /// Logs the command name and payload before and after the handler executes.
    /// </summary>
    /// <typeparam name="TRequest">The MediatR request type. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
    public class LoggingRequestWithResponseBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingRequestWithResponseBehavior<TRequest, TResponse>> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="LoggingRequestWithResponseBehavior{TRequest, TResponse}"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        public LoggingRequestWithResponseBehavior(ILogger<LoggingRequestWithResponseBehavior<TRequest, TResponse>> logger) => _logger = logger;


        /// <inheritdoc />
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _logger.LogInformation("----- Handling command {CommandName} ({@Command})", request.GetGenericTypeName(), request);
            var response = await next();
            _logger.LogInformation("----- Command {CommandName} handled - response: {@Response}", request.GetGenericTypeName(), response);

            return response;
        }
    }
}
