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
    public class ValidatorBehaviorForMediatR<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
    {
        private readonly IValidationService _validationService;
        private readonly ILogger<ValidatorBehaviorForMediatR<TRequest, TResponse>> _logger;

        public ValidatorBehaviorForMediatR(IValidationService validationService, ILogger<ValidatorBehaviorForMediatR<TRequest, TResponse>> logger)
        {
            _validationService = validationService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var typeName = request.GetGenericTypeName();

            _logger.LogInformation("----- Validating command {CommandType}", typeName);
            await _validationService.ValidateAsync<TRequest>(request, true, cancellationToken);
            return await next();
        }
    }

    public class ValidatorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IAppRequest<TResponse>
    {
        private readonly IValidationService _validationService;
        private readonly ILogger<ValidatorBehavior<TRequest, TResponse>> _logger;

        public ValidatorBehavior(IValidationService validationService, ILogger<ValidatorBehavior<TRequest, TResponse>> logger)
        {
            _validationService = validationService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var typeName = request.GetGenericTypeName();

            _logger.LogInformation("----- Validating command {CommandType}", typeName);
            await _validationService.ValidateAsync<TRequest>(request, true, cancellationToken);
            return await next();
        }
    }

}
