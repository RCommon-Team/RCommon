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
    public static class IPersistenceCachingBuilderExtensions
    {
        public static void AddInMemoryPersistenceCaching(this IPersistenceBuilder builder)
        {
            // Add Caching services
            builder.Services.TryAddTransient<Func<PersistenceCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case PersistenceCachingStrategy.Default:
                        return serviceProvider.GetService<InMemoryCacheService>();
                    default:
                        return serviceProvider.GetService<InMemoryCacheService>();
                }
            });
            builder.Services.TryAddTransient<ICommonFactory<PersistenceCachingStrategy, ICacheService>, CommonFactory<PersistenceCachingStrategy, ICacheService>>();
            ConfigureCachingOptions(builder);
        }

        public static void AddDistributedMemoryPersistenceCaching(this IPersistenceBuilder builder)
        {
            // Add Caching services
            builder.Services.TryAddTransient<Func<PersistenceCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case PersistenceCachingStrategy.Default:
                        return serviceProvider.GetService<DistributedMemoryCacheService>();
                    default:
                        return serviceProvider.GetService<DistributedMemoryCacheService>();
                }
            });
            builder.Services.TryAddTransient<ICommonFactory<PersistenceCachingStrategy, ICacheService>, CommonFactory<PersistenceCachingStrategy, ICacheService>>();
            ConfigureCachingOptions(builder);
            
        }

        private static void ConfigureCachingOptions(IPersistenceBuilder builder, Action<CachingOptions> configure = null)
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
