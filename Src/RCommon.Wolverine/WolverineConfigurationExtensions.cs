using Microsoft.Extensions.DependencyInjection;
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
            //config.Services.AddScoped<ISerializableEventPublisher, WolverineEventPublisher>();
            return config;
        }
    }
}
