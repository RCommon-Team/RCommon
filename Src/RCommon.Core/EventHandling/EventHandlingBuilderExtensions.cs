using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon
{
    public static class EventHandlingBuilderExtensions
    {
        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder)
            where T : IEventHandlingBuilder
        {
            return WithEventHandling<T>(builder, x => { });
        }

        public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : IEventHandlingBuilder
        {
            Guard.Against<UnsupportedDataStoreException>(dataStoreName.IsNullOrEmpty(), "You must set a name for the Data Store");

            this._services.TryAddTransient<IDataStoreFactory, DataStoreFactory>();
            this._services.Configure<DataStoreFactoryOptions>(options => options.Register<TDbContext>(dataStoreName));

            // Event Handling Configurations 
            var eventHandlingConfig = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(eventHandlingConfig);
            return builder;
        }

        public static void AddProducer<T>(this IEventHandlingBuilder builder) 
            where T : class, IEventProducer
        {
            builder.Services.AddSingleton<IEventProducer, T>();
        }

        public static void AddProducer<T>(this IEventHandlingBuilder builder, Func<IServiceProvider, T> getProducer) 
            where T : class, IEventProducer
        {
            builder.Services.AddSingleton(getProducer);
        }

        public static void AddProducer<T>(this IEventHandlingBuilder builder, T producer)
            where T : class, IEventProducer
        {
            builder.Services.TryAddSingleton(producer);
            builder.Services.TryAddSingleton<IEventProducer>(sp => sp.GetRequiredService<T>());

            if (producer is IHostedService service)
            {
                builder.Services.TryAddSingleton(service);
            }
        }
    }
}
