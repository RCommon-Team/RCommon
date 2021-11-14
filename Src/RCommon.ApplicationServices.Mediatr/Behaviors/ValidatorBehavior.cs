using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.MediatR.Behaviors
{
    public abstract class ValidatorBehavior<TRequest, TResponse, TException> : IPipelineBehavior<TRequest, TResponse> where TException : GeneralException, new()
    {
        private readonly ILogger<ValidatorBehavior<TRequest, TResponse, TException>> _logger;
        private readonly IValidator<TRequest>[] _validators;

        public ValidatorBehavior(IValidator<TRequest>[] validators, ILogger<ValidatorBehavior<TRequest, TResponse, TException>> logger)
        {
            _validators = validators;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var typeName = request.GetGenericTypeName();

            _logger.LogInformation("----- Validating command {CommandType}", typeName);

            var failures = _validators
                .Select(v => v.Validate(request))
                .SelectMany(result => result.Errors)
                .Where(error => error != null)
                .ToList();

            if (failures.Any())
            {
                _logger.LogWarning("Validation errors - {CommandType} - Command: {@Command} - Errors: {@ValidationErrors}", typeName, request, failures);
                string message = $"Command Validation Errors for type {typeof(TRequest).Name}";
                var ex = Activator.CreateInstance(typeof(TException), new object[] { message, new ValidationException("Validation exception", failures) }) as TException;
                throw ex;
            }

            return await next();
        }
    }
}
