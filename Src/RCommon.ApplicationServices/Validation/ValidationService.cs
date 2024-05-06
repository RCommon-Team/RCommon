using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace RCommon.ApplicationServices.Validation
{
    public class ValidationService : IValidationService
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<ValidationOutcome> ValidateAsync<T>(T target, bool throwOnFaults = false, CancellationToken cancellationToken = default)
            where T : class
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var provider = scope.ServiceProvider.GetService<IValidationProvider>();
                Guard.IsNotNull(provider, nameof(provider));
                var outcome = await provider.ValidateAsync<T>(target, throwOnFaults, cancellationToken);
                return outcome;
            }
        }
    }
}
