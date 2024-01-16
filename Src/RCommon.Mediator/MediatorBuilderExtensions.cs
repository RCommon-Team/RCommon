using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public static class MediatorBuilderExtensions
    {
        public static void AddEvent<T>(this IEventHandlingBuilder config)
            where T : class, ISerializableEvent, IAppNotification
        {
            config.Services.TryAddTransient<T>();
        }

        public static void AddEvent<T>(this IEventHandlingBuilder config, Func<IServiceProvider, T> getEvent)
            where T : class, ISerializableEvent, IAppNotification
        {
            config.Services.TryAddTransient(getEvent);
        }

        
    }
}
