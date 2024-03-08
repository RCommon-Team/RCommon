using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Wolverine
{
    public class WolverineEventHandlingBuilder : IWolverineEventHandlingBuilder
    {
        public WolverineEventHandlingBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
        }

        public IServiceCollection Services { get; }
    }
}
