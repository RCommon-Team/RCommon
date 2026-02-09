using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RCommon.ApplicationServices.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    /// <summary>
    /// Extension methods for <see cref="IRCommonBuilder"/> and <see cref="IValidationBuilder"/> that provide
    /// fluent registration of validation infrastructure.
    /// </summary>
    public static class ValidationBuilderExtensions
    {
        /// <summary>
        /// Adds validation support using the specified <see cref="IValidationBuilder"/> implementation with default configuration.
        /// </summary>
        /// <typeparam name="T">The <see cref="IValidationBuilder"/> implementation type to use.</typeparam>
        /// <param name="builder">The RCommon builder.</param>
        /// <returns>The <paramref name="builder"/> for further chaining.</returns>
        public static IRCommonBuilder WithValidation<T>(this IRCommonBuilder builder)
            where T : IValidationBuilder
        {
            return WithValidation<T>(builder, x => { });
        }

        /// <summary>
        /// Adds validation support using the specified <see cref="IValidationBuilder"/> implementation and applies additional configuration.
        /// </summary>
        /// <typeparam name="T">The <see cref="IValidationBuilder"/> implementation type to use.</typeparam>
        /// <param name="builder">The RCommon builder.</param>
        /// <param name="actions">A delegate to configure the validation builder (e.g., register validation providers).</param>
        /// <returns>The <paramref name="builder"/> for further chaining.</returns>
        public static IRCommonBuilder WithValidation<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IValidationBuilder
        {

            builder.Services.AddScoped<IValidationService, ValidationService>();

            // Instantiate the validation builder implementation, which may register provider-specific services
            var mediatorConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder })!;
            actions(mediatorConfig);
            return builder;
        }

        /// <summary>
        /// Configures the validation builder to integrate with the CQRS pipeline by setting
        /// <see cref="CqrsValidationOptions"/> (e.g., enabling command or query validation).
        /// </summary>
        /// <param name="builder">The validation builder.</param>
        /// <param name="options">A delegate to configure <see cref="CqrsValidationOptions"/>.</param>
        public static void UseWithCqrs(this IValidationBuilder builder, Action<CqrsValidationOptions> options)
        {
            builder.Services.Configure<CqrsValidationOptions>(options);
        }

    }
}
