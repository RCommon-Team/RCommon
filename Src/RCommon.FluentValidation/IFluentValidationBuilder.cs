using Microsoft.Extensions.DependencyInjection;
using RCommon.ApplicationServices;

namespace RCommon.FluentValidation
{
    /// <summary>
    /// Builder interface for configuring validation using the FluentValidation library
    /// within the RCommon framework.
    /// </summary>
    /// <seealso cref="IValidationBuilder"/>
    /// <seealso cref="FluentValidationBuilder"/>
    public interface IFluentValidationBuilder : IValidationBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> used to register FluentValidation services and validators.
        /// </summary>
        new IServiceCollection Services { get; }
    }
}
