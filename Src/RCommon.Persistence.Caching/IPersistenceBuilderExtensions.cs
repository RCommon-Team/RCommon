using Microsoft.Extensions.DependencyInjection;
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
        public static IPersistenceBuilder EnablePersistenceCaching(this IPersistenceBuilder builder)
        {
            builder.Services.AddTransient(typeof(ICachingGraphRepository<>), typeof(CachingGraphRepository<>));
            builder.Services.AddTransient(typeof(ICachingLinqRepository<>), typeof(CachingLinqRepository<>));
            builder.Services.AddTransient(typeof(ICachingSqlMapperRepository<>), typeof(CachingSqlMapperRepository<>));
            return builder;
        }
    }
}
