using MediatR;
using Microsoft.Extensions.Logging;
using RCommon.ApplicationServices.Validation;
using RCommon.Mediator.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR.Behaviors
{
    /// <summary>
    /// MediatR pipeline behavior that validates requests implementing <see cref="IRequest{TResponse}"/>
    /// before they reach the handler. Uses <see cref="IValidationService"/> to perform validation,
    /// throwing on failure to prevent invalid requests from being processed.
    /// </summary>
    /// <typeparam name="TRequest">The MediatR request type. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
    public class ValidatorBehaviorForMediatR<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
    {
        private readonly IValidationService _validationService;
        private readonly ILogger<ValidatorBehaviorForMediatR<TRequest, TResponse>> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="ValidatorBehaviorForMediatR{TRequest, TResponse}"/>.
        /// </summary>
        /// <param name="validationService">The validation service used to validate the request.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public ValidatorBehaviorForMediatR(IValidationService validationService, ILogger<ValidatorBehaviorForMediatR<TRequest, TResponse>> logger)
        {
            _validationService = validationService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var typeName = request.GetGenericTypeName();

            _logger.LogInformation("----- Validating command {CommandType}", typeName);
            // Validate the request and throw on failure (second param = throwOnFailure)
            await _validationService.ValidateAsync<TRequest>(request, true, cancellationToken);
            return await next();
        }
    }

    /// <summary>
    /// MediatR pipeline behavior that validates requests implementing <see cref="IAppRequest{TResponse}"/>
    /// before they reach the handler. Uses <see cref="IValidationService"/> to perform validation,
    /// throwing on failure to prevent invalid requests from being processed.
    /// </summary>
    /// <typeparam name="TRequest">The RCommon application request type. Must implement <see cref="IAppRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
    public class ValidatorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IAppRequest<TResponse>
    {
        private readonly IValidationService _validationService;
        private readonly ILogger<ValidatorBehavior<TRequest, TResponse>> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="ValidatorBehavior{TRequest, TResponse}"/>.
        /// </summary>
        /// <param name="validationService">The validation service used to validate the request.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public ValidatorBehavior(IValidationService validationService, ILogger<ValidatorBehavior<TRequest, TResponse>> logger)
        {
            _validationService = validationService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var typeName = request.GetGenericTypeName();

            _logger.LogInformation("----- Validating command {CommandType}", typeName);
            // Validate the request and throw on failure (second param = throwOnFailure)
            await _validationService.ValidateAsync<TRequest>(request, true, cancellationToken);
            return await next();
        }
    }

}
