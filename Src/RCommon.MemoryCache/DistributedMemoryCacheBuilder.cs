using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MemoryCache
{
    /// <summary>
    /// Builder for configuring distributed memory caching using the Microsoft
    /// <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/> abstraction
    /// backed by an in-memory store.
    /// </summary>
    /// <remarks>
    /// This is the concrete builder activated by
    /// <see cref="CachingBuilderExtensions.WithDistributedCaching{T}(IRCommonBuilder, Action{T})"/>
    /// when <c>DistributedMemoryCacheBuilder</c> is specified as the type parameter.
    /// </remarks>
    public class DistributedMemoryCacheBuilder : IDistributedMemoryCachingBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedMemoryCacheBuilder"/> class.
        /// </summary>
        /// <param name="builder">The RCommon builder whose <see cref="IServiceCollection"/> is used for service registration.</param>
        public DistributedMemoryCacheBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            this.RegisterServices(Services);
        }

        /// <summary>
        /// Registers any default services required by the distributed memory cache builder.
        /// </summary>
        /// <param name="services">The service collection to register services into.</param>
        protected void RegisterServices(IServiceCollection services)
        {

        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
