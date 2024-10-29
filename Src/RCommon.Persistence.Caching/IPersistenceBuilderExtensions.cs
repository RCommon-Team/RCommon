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
    public static class IPersistenceBuilderExtensions
    {
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
