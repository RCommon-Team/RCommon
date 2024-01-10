using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling.Producers;
using RCommon.Messaging;
using RCommon.Messaging.Wolverine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wolverine;

namespace RCommon
{
    public static class WolverineConfigurationExtensions
    {
        public static IRCommonConfiguration WithWolverine(this IRCommonConfiguration config, Action<WolverineOptions> options)
        {
            config.Services.AddScoped<IDistributedEventPublisher, WolverineEventPublisher>();
            return config;
        }
    }
}
