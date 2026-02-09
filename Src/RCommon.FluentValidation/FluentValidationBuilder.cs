using Microsoft.Extensions.DependencyInjection;
using RCommon.ApplicationServices.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace RCommon.FluentValidation
{
    /// <summary>
    /// Default implementation of <see cref="IFluentValidationBuilder"/> that registers
    /// the <see cref="FluentValidationProvider"/> as the <see cref="IValidationProvider"/>
    /// in the DI container.
    /// </summary>
    /// <seealso cref="IFluentValidationBuilder"/>
    /// <seealso cref="FluentValidationProvider"/>
    public class FluentValidationBuilder : IFluentValidationBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FluentValidationBuilder"/> and registers
        /// FluentValidation services into the DI container.
        /// </summary>
        /// <param name="builder">The RCommon builder providing access to the <see cref="IServiceCollection"/>.</param>
        public FluentValidationBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            this.RegisterServices(Services);
        }

        /// <summary>
        /// Registers the <see cref="FluentValidationProvider"/> as the <see cref="IValidationProvider"/>
        /// implementation with a scoped lifetime.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        protected void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IValidationProvider, FluentValidationProvider>();
        }

        /// <inheritdoc/>
        public IServiceCollection Services { get; }
    }
}
