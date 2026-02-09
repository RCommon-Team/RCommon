using Microsoft.Extensions.DependencyInjection;
using RCommon.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.RedisCache
{
    /// <summary>
    /// Builder for configuring Redis-backed distributed caching using the StackExchange.Redis provider.
    /// </summary>
    /// <remarks>
    /// This is the concrete builder activated by
    /// <see cref="Caching.CachingBuilderExtensions.WithDistributedCaching{T}(IRCommonBuilder, Action{T})"/>
    /// when <c>RedisCachingBuilder</c> is specified as the type parameter.
    /// </remarks>
    public class RedisCachingBuilder : IRedisCachingBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCachingBuilder"/> class.
        /// </summary>
        /// <param name="builder">The RCommon builder whose <see cref="IServiceCollection"/> is used for service registration.</param>
        public RedisCachingBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            this.RegisterServices(Services);
        }

        /// <summary>
        /// Registers any default services required by the Redis cache builder.
        /// </summary>
        /// <param name="services">The service collection to register services into.</param>
        protected void RegisterServices(IServiceCollection services)
        {

        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
