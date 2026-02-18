using Microsoft.Extensions.DependencyInjection;
using System;

namespace RCommon.MultiTenancy
{
    /// <summary>
    /// Extension methods for <see cref="IRCommonBuilder"/> that register multitenancy services.
    /// </summary>
    public static class MultiTenancyBuilderExtensions
    {
        /// <summary>
        /// Adds a multitenancy provider and applies the specified configuration actions.
        /// </summary>
        /// <typeparam name="TBuilder">The <see cref="IMultiTenantBuilder"/> implementation to configure (e.g., Finbuckle).</typeparam>
        /// <param name="builder">The RCommon builder instance.</param>
        /// <param name="actions">An action to configure the multitenancy provider.</param>
        /// <returns>The <see cref="IRCommonBuilder"/> for fluent chaining.</returns>
        public static IRCommonBuilder WithMultiTenancy<TBuilder>(this IRCommonBuilder builder, Action<TBuilder> actions)
            where TBuilder : IMultiTenantBuilder
        {
            var multiTenantBuilder = (TBuilder)Activator.CreateInstance(typeof(TBuilder), new object[] { builder.Services })!;
            actions(multiTenantBuilder);
            return builder;
        }
    }
}
