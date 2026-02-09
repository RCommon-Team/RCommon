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
    /// <summary>
    /// Implements <see cref="IValidationProvider"/> using the FluentValidation library.
    /// Resolves registered <see cref="IValidator{T}"/> instances from the DI container
    /// and executes them against the target object.
    /// </summary>
    /// <seealso cref="IValidationProvider"/>
    /// <seealso cref="FluentValidationBuilder"/>
    public class FluentValidationProvider : IValidationProvider
    {
        private readonly ILogger<FluentValidationProvider> _logger;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="FluentValidationProvider"/>.
        /// </summary>
        /// <param name="logger">The logger for recording validation warnings and errors.</param>
        /// <param name="serviceProvider">The service provider used to resolve validator instances.</param>
        public FluentValidationProvider(ILogger<FluentValidationProvider> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Validates the specified target object by resolving and executing all registered
        /// <see cref="IValidator{T}"/> instances for the target's type.
        /// </summary>
        /// <typeparam name="T">The type of the object being validated.</typeparam>
        /// <param name="target">The object to validate.</param>
        /// <param name="throwOnFaults">
        /// When <see langword="true"/>, a <see cref="ApplicationServices.Validation.ValidationException"/>
        /// is thrown if any validation failures are found.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the async operation.</param>
        /// <returns>A <see cref="ValidationOutcome"/> containing any validation faults.</returns>
        /// <exception cref="ApplicationServices.Validation.ValidationException">
        /// Thrown when validation fails and <paramref name="throwOnFaults"/> is <see langword="true"/>.
        /// </exception>
        public async Task<ValidationOutcome> ValidateAsync<T>(T target, bool throwOnFaults, CancellationToken cancellationToken = default)
            where T : class
        {
            var outcome = new ValidationOutcome();

            using (var scope = _serviceProvider.CreateScope())
            {
                // Build the closed generic IValidator<T> type using the runtime type of the target
                // so that validators registered for derived types are also resolved
                var type = target.GetType();
                var validatorType = typeof(IValidator<>).MakeGenericType(type);
                var untypedValidators = scope.ServiceProvider.GetServices(validatorType);

                Guard.IsNotNull(untypedValidators, nameof(untypedValidators));

                var validationResults = await ExecuteValidationAsync(target, untypedValidators!, cancellationToken); // TODO: Need a better way than passing in object[]

                // Flatten all validation errors from all validators into a single list
                var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

                if (failures.Count != 0)
                {
                    _logger.LogWarning("Validation errors - {CommandType} - Command: {@Command} - Errors: {@ValidationErrors}", target.GetGenericTypeName(), target, failures);
                    string message = $"Validation Errors";

                    // Map FluentValidation failures to RCommon ValidationFault instances
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

        /// <summary>
        /// Executes all provided validators against the target object concurrently.
        /// </summary>
        /// <typeparam name="T">The type of the object being validated.</typeparam>
        /// <param name="target">The object to validate.</param>
        /// <param name="validators">The collection of untyped validator instances resolved from DI.</param>
        /// <param name="cancellationToken">A token to cancel the async operation.</param>
        /// <returns>An array of <see cref="ValidationResult"/> from all executed validators.</returns>
        private async Task<ValidationResult[]> ExecuteValidationAsync<T>(T target, IEnumerable<object> validators, CancellationToken cancellationToken = default)
            where T : class
        {
            if (validators.Any())
            {
                var context = new ValidationContext<T>(target);

                // Run all validators in parallel via Task.WhenAll, casting each to the non-generic IValidator interface
                var validationResults = await Task.WhenAll(validators.Select(v => ((IValidator)v).ValidateAsync(context, cancellationToken)));
                return validationResults;
            }
            else
            {
                // No validators registered for this type; return an empty result set
                return new List<ValidationResult>().ToArray();
            }
        }
    }
}
