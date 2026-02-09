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
        /// Registers validation using the specified <typeparamref name="T"/> builder with default
        /// <see cref="CqrsValidationOptions"/>.
        /// </summary>
        /// <typeparam name="T">An <see cref="IValidationBuilder"/> implementation such as
        /// <c>FluentValidationBuilder</c>.</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
        public static IRCommonBuilder WithValidation<T>(this IRCommonBuilder builder)
            where T : IValidationBuilder
        {

            return WithValidation<T>(builder, x => { });
        }

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
        public static IRCommonBuilder WithValidation<T>(this IRCommonBuilder builder, Action<CqrsValidationOptions> actions)
            where T : IValidationBuilder
        {

            // Instantiate the validation builder via reflection; the constructor registers validation services into DI
            var cqrsBuilder = (T)Activator.CreateInstance(typeof(T), new object[] { builder })!;
            builder.Services.Configure<CqrsValidationOptions>(actions);
            return builder;
        }
    }
}
