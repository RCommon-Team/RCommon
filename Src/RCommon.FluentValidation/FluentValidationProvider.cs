using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.ApplicationServices.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.FluentValidation
{
    public class FluentValidationProvider : IValidationProvider  
    {
        private readonly ILogger<FluentValidationProvider> _logger;
        private readonly IServiceProvider _serviceProvider;

        public FluentValidationProvider(ILogger<FluentValidationProvider> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<ValidationOutcome> ValidateAsync<T>(T target, bool throwOnFaults, CancellationToken cancellationToken = default)
            where T : class
        {
            var outcome = new ValidationOutcome();

            using (var scope = _serviceProvider.CreateScope())
            {
                var type = target.GetType();
                var validatorType = typeof(IValidator<>).MakeGenericType(type);
                var untypedValidators = scope.ServiceProvider.GetServices(validatorType);
                
                Guard.IsNotNull(untypedValidators, nameof(untypedValidators));

                var validationResults = await ExecuteValidationAsync(target, untypedValidators, cancellationToken); // TODO: Need a better way than passing in object[]
                var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

                if (failures.Count != 0)
                {
                    _logger.LogWarning("Validation errors - {CommandType} - Command: {@Command} - Errors: {@ValidationErrors}", target.GetGenericTypeName(), target, failures);
                    string message = $"Validation Errors";

                    var faults = new List<ValidationFault>();
                    foreach (var failure in failures)
                    {
                        faults.Add(new ValidationFault(failure.PropertyName, failure.ErrorMessage, failure.AttemptedValue));
                    }

                    outcome.Errors = faults;

                    if (throwOnFaults)
                    {
                        throw new ApplicationServices.Validation.ValidationException(message, faults, true);
                    }

                }
            }
            return outcome;
        }

        private async Task<ValidationResult[]> ExecuteValidationAsync<T>(T target, IEnumerable<object> validators, CancellationToken cancellationToken = default)
            where T : class
        {
            if (validators.Any())
            {     
                var context = new ValidationContext<T>(target);
                var validationResults = await Task.WhenAll(validators.Select(v => ((IValidator)v).ValidateAsync(context, cancellationToken)));
                return validationResults;
            }
            else
            {
                return new List<ValidationResult>().ToArray();
            }
        }
    }
}
