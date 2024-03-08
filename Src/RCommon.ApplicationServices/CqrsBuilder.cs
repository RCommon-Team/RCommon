using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using RCommon.ApplicationServices.Commands;
using RCommon.ApplicationServices.Queries;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    public class CqrsBuilder : ICqrsBuilder
    {
        public CqrsBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            this.RegisterServices(Services);
        }

        protected void RegisterServices(IServiceCollection services)
        {
            services.AddTransient<ICommandBus, CommandBus>();
            services.AddTransient<IQueryBus, QueryBus>();
            services.AddTransient<IMemoryCache, MemoryCache>();
        }

        public IServiceCollection Services { get; }
    }
}
