using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling
{
    public class InMemoryEventBusBuilder : IInMemoryEventBusBuilder
    {

        public InMemoryEventBusBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;

        }

        public IServiceCollection Services { get; }
    }
}

