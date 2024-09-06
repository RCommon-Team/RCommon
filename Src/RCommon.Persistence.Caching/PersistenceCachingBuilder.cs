using Microsoft.Extensions.DependencyInjection;
using RCommon.Caching;
using RCommon.Persistence.Caching.Crud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching
{
    public class PersistenceCachingBuilder : IPersistenceCachingBuilder
    {
        public PersistenceCachingBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            RegisterServices(Services);
        }

        protected void RegisterServices(IServiceCollection services)
        {
            services.AddTransient(typeof(ICachingGraphRepository<>), typeof(CachingGraphRepository<>));
            services.AddTransient(typeof(ICachingLinqRepository<>), typeof(CachingLinqRepository<>));
            services.AddTransient(typeof(ICachingSqlMapperRepository<>), typeof(CachingSqlMapperRepository<>));
        }

        public IServiceCollection Services { get; }
    }
}
