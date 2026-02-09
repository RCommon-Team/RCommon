using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    /// <summary>
    /// Defines the contract for a builder that configures validation services.
    /// </summary>
    /// <remarks>
    /// Implementations register <see cref="Validation.IValidationProvider"/> and related services
    /// into the DI container. Use <see cref="ValidationBuilderExtensions.UseWithCqrs"/> to integrate
    /// validation with the CQRS pipeline.
    /// </remarks>
    public interface IValidationBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> used to register validation-related services.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
