using Microsoft.Extensions.DependencyInjection;
using System;

namespace RCommon
{
    /// <summary>
    /// Base interface implemented by specific data configurators that configure RCommon data providers.
    /// </summary>
    /// <remarks>
    /// Concrete implementations (e.g., EF Core, Dapper, MongoDB builders) register provider-specific services
    /// and data stores into the DI container via the <see cref="Services"/> collection.
    /// </remarks>
    public interface IPersistenceBuilder
    {
        /// <summary>
        /// Sets the default data store that repositories will use when no explicit data store name is specified.
        /// </summary>
        /// <param name="options">An action to configure the <see cref="DefaultDataStoreOptions"/>.</param>
        /// <returns>The current <see cref="IPersistenceBuilder"/> instance for fluent chaining.</returns>
        IPersistenceBuilder SetDefaultDataStore(Action<DefaultDataStoreOptions> options);

        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> used to register persistence-related services.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
