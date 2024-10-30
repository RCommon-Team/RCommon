using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.Caching;
using RCommon.Persistence.Caching.Crud;
using RCommon.RedisCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching.RedisCache
{
    public static class IPersistenceBuilderExtensions
    {

        public static void AddRedisPersistenceCaching(this IPersistenceBuilder builder)
        {
            // Add Caching repositories
            builder.Services.TryAddTransient<ICacheService, RedisCacheService>();
            builder.Services.TryAddTransient<RedisCacheService>();
            builder.Services.TryAddTransient<ICommonFactory<PersistenceCachingStrategy, ICacheService>, CommonFactory<PersistenceCachingStrategy, ICacheService>>();
            ConfigureCachingOptions(builder);

            // Add Caching services
            builder.Services.TryAddTransient<Func<PersistenceCachingStrategy, ICacheService>>(serviceProvider => strategy =>
            {
                switch (strategy)
                {
                    case PersistenceCachingStrategy.Default:
                        return serviceProvider.GetService<RedisCacheService>();
                    default:
                        return serviceProvider.GetService<RedisCacheService>();
                }
            });
        }

        private static void ConfigureCachingOptions(IPersistenceBuilder builder, Action<CachingOptions> configure = null)
        {
            // Add Caching repositories
            builder.Services.TryAddTransient(typeof(ICachingGraphRepository<>), typeof(CachingGraphRepository<>));
            builder.Services.TryAddTransient(typeof(ICachingLinqRepository<>), typeof(CachingLinqRepository<>));
            builder.Services.TryAddTransient(typeof(ICachingSqlMapperRepository<>), typeof(CachingSqlMapperRepository<>));

            if (configure == null)
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
