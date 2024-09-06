using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching
{
    public static class IPersistenceCachingBuilderExtensions
    {
        public static IPersistenceCachingBuilder Configure(this IPersistenceCachingBuilder builder, Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory)
        {
            builder.Services.TryAddTransient<Func<PersistenceCachingStrategy, ICacheService>>(cacheFactory);
            builder.Services.TryAddTransient<ICommonFactory<PersistenceCachingStrategy, ICacheService>, CommonFactory<PersistenceCachingStrategy, ICacheService>>();

            builder.Services.Configure<CachingOptions>(x =>
            {
                x.CachingEnabled = true;
                x.CacheDynamicallyCompiledExpressions = true;
            });
            return builder;
        }
    }
}
