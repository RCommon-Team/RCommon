using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace RCommon.ApplicationServices.Validation
{
    /// <summary>
    /// Default implementation of <see cref="IValidationService"/> that delegates validation
    /// to a scoped <see cref="IValidationProvider"/>.
    /// </summary>
    /// <remarks>
    /// A new DI scope is created for each validation call to ensure that scoped validation
    /// providers (and their dependencies) are properly resolved and disposed.
    /// </remarks>
    public class ValidationService : IValidationService
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="ValidationService"/>.
        /// </summary>
        /// <param name="serviceProvider">The root service provider used to create scoped validation providers.</param>
        public ValidationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public async Task<ValidationOutcome> ValidateAsync<T>(T target, bool throwOnFaults = false, CancellationToken cancellationToken = default)
            where T : class
        {
            // Create a new scope so that scoped IValidationProvider instances are properly resolved
            using (var scope = _serviceProvider.CreateScope())
            {
                var provider = scope.ServiceProvider.GetService<IValidationProvider>();
                Guard.IsNotNull(provider!, nameof(provider));
                var outcome = await provider!.ValidateAsync<T>(target, throwOnFaults, cancellationToken);
                return outcome;
            }
        }
    }
}
