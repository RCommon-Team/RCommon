using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.Caching;
using RCommon.Persistence.Caching.Crud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching
{
    /// <summary>
    /// Extension methods on <see cref="IPersistenceBuilder"/> for registering persistence-level caching
    /// with a custom <see cref="ICacheService"/> factory.
    /// </summary>
    public static class IPersistenceBuilderExtensions
    {
        /// <summary>
        /// Registers persistence caching infrastructure including the cache service factory,
        /// all caching repository decorators, and default <see cref="CachingOptions"/>.
        /// </summary>
        /// <param name="builder">The persistence builder.</param>
        /// <param name="cacheFactory">
        /// A factory that, given an <see cref="IServiceProvider"/>, returns a delegate resolving
        /// <see cref="ICacheService"/> from a <see cref="PersistenceCachingStrategy"/>.
        /// </param>
        /// <remarks>
        /// This overload lets callers supply their own cache factory delegate, which is useful when
        /// the caching provider is not one of the built-in memory or Redis implementations.
        /// </remarks>
        public static void AddPersistenceCaching(this IPersistenceBuilder builder, Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory)
        {
            // Add caching services
            builder.Services.TryAddTransient<Func<PersistenceCachingStrategy, ICacheService>>(cacheFactory);
            builder.Services.TryAddTransient<ICommonFactory<PersistenceCachingStrategy, ICacheService>, CommonFactory<PersistenceCachingStrategy, ICacheService>>();

            // Add Caching repositories
            builder.Services.TryAddTransient(typeof(ICachingGraphRepository<>), typeof(CachingGraphRepository<>));
            builder.Services.TryAddTransient(typeof(ICachingLinqRepository<>), typeof(CachingLinqRepository<>));
            builder.Services.TryAddTransient(typeof(ICachingSqlMapperRepository<>), typeof(CachingSqlMapperRepository<>));

            builder.Services.Configure<CachingOptions>(x =>
            {
                x.CachingEnabled = true;
                x.CacheDynamicallyCompiledExpressions = true;
            });
        }
    }
}
