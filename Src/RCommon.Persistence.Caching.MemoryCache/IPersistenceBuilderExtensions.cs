using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.Caching;
using RCommon.MemoryCache;
using RCommon.Persistence.Caching.Crud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching.MemoryCache
{
    /// <summary>
    /// Extension methods on <see cref="IPersistenceBuilder"/> for registering memory-based
    /// persistence caching (both in-process and distributed memory).
    /// </summary>
    public static class IPersistenceBuilderExtensions
    {
        /// <summary>
        /// Registers persistence caching backed by the in-process <see cref="InMemoryCacheService"/>,
        /// including all caching repository decorators and the strategy-based cache factory.
        /// </summary>
        /// <param name="builder">The persistence builder.</param>
        public static void AddInMemoryPersistenceCaching(this IPersistenceBuilder builder)
        {
            // Add Caching services
            builder.Services.TryAddTransient<ICacheService, InMemoryCacheService>();
            builder.Services.TryAddTransient<InMemoryCacheService>();
            builder.Services.TryAddTransient<ICommonFactory<PersistenceCachingStrategy, ICacheService>, CommonFactory<PersistenceCachingStrategy, ICacheService>>();
            ConfigureCachingOptions(builder);

            // Add Caching Factory — resolves the correct ICacheService based on the PersistenceCachingStrategy
            builder.Services.TryAddTransient<Func<PersistenceCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case PersistenceCachingStrategy.Default:
                        return serviceProvider.GetRequiredService<InMemoryCacheService>();
                    default:
                        return serviceProvider.GetRequiredService<InMemoryCacheService>();
                }
            });

        }

        /// <summary>
        /// Registers persistence caching backed by the <see cref="DistributedMemoryCacheService"/>
        /// (an in-memory distributed cache), including all caching repository decorators
        /// and the strategy-based cache factory.
        /// </summary>
        /// <param name="builder">The persistence builder.</param>
        public static void AddDistributedMemoryPersistenceCaching(this IPersistenceBuilder builder)
        {
            // Add Caching services
            builder.Services.TryAddTransient<ICacheService, DistributedMemoryCacheService>();
            builder.Services.TryAddTransient<DistributedMemoryCacheService>();
            builder.Services.TryAddTransient<ICommonFactory<PersistenceCachingStrategy, ICacheService>, CommonFactory<PersistenceCachingStrategy, ICacheService>>();
            ConfigureCachingOptions(builder);

            // Add Caching Factory — resolves the correct ICacheService based on the PersistenceCachingStrategy
            builder.Services.TryAddTransient<Func<PersistenceCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case PersistenceCachingStrategy.Default:
                        return serviceProvider.GetRequiredService<DistributedMemoryCacheService>();
                    default:
                        return serviceProvider.GetRequiredService<DistributedMemoryCacheService>();
                }
            });
        }

        /// <summary>
        /// Registers the open-generic caching repository decorators and configures <see cref="CachingOptions"/>
        /// with default or custom settings.
        /// </summary>
        /// <param name="builder">The persistence builder.</param>
        /// <param name="configure">An optional delegate to customize <see cref="CachingOptions"/>. When <c>null</c>, defaults are applied.</param>
        private static void ConfigureCachingOptions(IPersistenceBuilder builder, Action<CachingOptions>? configure = null)
        {
            // Add Caching repositories
            builder.Services.TryAddTransient(typeof(ICachingGraphRepository<>), typeof(CachingGraphRepository<>));
            builder.Services.TryAddTransient(typeof(ICachingLinqRepository<>), typeof(CachingLinqRepository<>));
            builder.Services.TryAddTransient(typeof(ICachingSqlMapperRepository<>), typeof(CachingSqlMapperRepository<>));

            if ( configure ==  null)
            {
                builder.Services.Configure<CachingOptions>(x =>
                {
                    x.CachingEnabled = true;
                    x.CacheDynamicallyCompiledExpressions = true;
                });
            }
            else
            {
                builder.Services.Configure(configure);
            }

        }
    }
}
