using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling
{
    public class InMemoryEventBusBuilder : IEventHandlingBuilder
    {

        public InMemoryEventBusBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;

            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                return new InMemoryEventBus(sp, builder.Services);
            });

        }

        public IServiceCollection Services { get; }
    }
}

