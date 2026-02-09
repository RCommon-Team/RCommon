using Microsoft.Extensions.DependencyInjection;
using RCommon.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MemoryCache
{
    /// <summary>
    /// Builder for configuring in-memory caching using the Microsoft
    /// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> abstraction.
    /// </summary>
    /// <remarks>
    /// This is the concrete builder activated by
    /// <see cref="Caching.CachingBuilderExtensions.WithMemoryCaching{T}(IRCommonBuilder, Action{T})"/>
    /// when <c>InMemoryCachingBuilder</c> is specified as the type parameter.
    /// </remarks>
    public class InMemoryCachingBuilder : IInMemoryCachingBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCachingBuilder"/> class.
        /// </summary>
        /// <param name="builder">The RCommon builder whose <see cref="IServiceCollection"/> is used for service registration.</param>
        public InMemoryCachingBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            this.RegisterServices(Services);
        }

        /// <summary>
        /// Registers any default services required by the in-memory cache builder.
        /// </summary>
        /// <param name="services">The service collection to register services into.</param>
        protected void RegisterServices(IServiceCollection services)
        {

        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
