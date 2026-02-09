using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RCommon.EventHandling;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Provides extension methods on <see cref="IRCommonBuilder"/> for registering a mediator implementation.
    /// </summary>
    public static class MediatorBuilderExtensions
    {
        /// <summary>
        /// Registers a mediator implementation using default configuration.
        /// </summary>
        /// <typeparam name="T">
        /// The <see cref="IMediatorBuilder"/> implementation that configures the specific mediator library.
        /// </typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for chaining additional configuration calls.</returns>
        public static IRCommonBuilder WithMediator<T>(this IRCommonBuilder builder)
            where T : IMediatorBuilder
        {
            return WithMediator<T>(builder, x => { });
        }

        /// <summary>
        /// Registers a mediator implementation with custom configuration.
        /// </summary>
        /// <typeparam name="T">
        /// The <see cref="IMediatorBuilder"/> implementation that configures the specific mediator library.
        /// </typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <param name="actions">A delegate to configure the mediator builder of type <typeparamref name="T"/>.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for chaining additional configuration calls.</returns>
        /// <remarks>
        /// This method registers <see cref="MediatorService"/> as the scoped <see cref="IMediatorService"/> implementation,
        /// then creates the mediator builder via <see cref="Activator.CreateInstance(Type, object[])"/> and invokes
        /// the configuration delegate to allow library-specific setup.
        /// </remarks>
        public static IRCommonBuilder WithMediator<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IMediatorBuilder
        {

            builder.Services.AddScoped<IMediatorService, MediatorService>();

            // Create the mediator-specific builder by convention (expects a constructor accepting IRCommonBuilder)
            var mediatorConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder })!;
            actions(mediatorConfig);
            return builder;
        }

    }
}
