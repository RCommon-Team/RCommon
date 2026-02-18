using Microsoft.Extensions.DependencyInjection;

namespace RCommon.MultiTenancy
{
    /// <summary>
    /// Defines the builder interface for configuring multitenancy services.
    /// Concrete implementations (e.g., Finbuckle) register their tenant resolution
    /// and <see cref="RCommon.Persistence.Crud.ITenantIdAccessor"/> implementations through this builder.
    /// </summary>
    public interface IMultiTenantBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> used to register multitenancy services.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
