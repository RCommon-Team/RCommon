using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    /// <summary>
    /// Provides extension methods on <see cref="IRCommonBuilder"/> for registering validation
    /// into the RCommon configuration pipeline.
    /// </summary>
    public static class ValidationBuilderExtensions
    {

        /// <summary>
        /// Registers validation using the specified <typeparamref name="T"/> builder and configures
        /// <see cref="CqrsValidationOptions"/> for CQRS pipeline validation behavior.
        /// </summary>
        /// <typeparam name="T">An <see cref="IValidationBuilder"/> implementation.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <param name="actions">An action to configure <see cref="CqrsValidationOptions"/>.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        /// <remarks>
        /// This method uses <see cref="Activator.CreateInstance(Type, object[])"/> to instantiate the builder,
        /// passing the <see cref="IRCommonBuilder"/> as the constructor argument. The builder's constructor
        /// is expected to register its validation services into the DI container.
        /// </remarks>
        [Obsolete("Use RCommon.ApplicationServices.ValidationBuilderExtensions.WithValidation<T>(Action<T>) " +
            "and call UseWithCqrs(Action<CqrsValidationOptions>) inside that lambda instead, e.g. " +
            "builder.WithValidation<T>(v => v.UseWithCqrs(opts => opts.ValidateCommands = true)). " +
            "This overload will be removed in a future major version.")]
        public static IRCommonBuilder WithValidation<T>(this IRCommonBuilder builder, Action<CqrsValidationOptions> actions)
            where T : class, IValidationBuilder
        {

            // Instantiate the validation builder via reflection; the constructor registers validation services into DI.
            // Routed through GetOrAddBuilder so repeated WithValidation<T> calls reuse the cached sub-builder.
            var cqrsBuilder = builder.GetOrAddBuilder<T>(
                () => (T)Activator.CreateInstance(typeof(T), new object[] { builder })!);
            builder.Services.Configure<CqrsValidationOptions>(actions);
            return builder;
        }
    }
}
