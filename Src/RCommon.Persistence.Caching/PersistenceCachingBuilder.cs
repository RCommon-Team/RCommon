using Microsoft.Extensions.DependencyInjection;
using RCommon.Caching;
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

        }

        public IServiceCollection Services { get; }
    }
}
