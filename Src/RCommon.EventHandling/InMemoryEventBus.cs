using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling.Subscribers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.EventHandling
{
    public class InMemoryEventBus : IEventBus
    {
        private readonly IServiceCollection _services;
        private readonly IServiceProvider _serviceProvider;

        public InMemoryEventBus(IServiceProvider serviceProvider, IServiceCollection services)
        {
            _serviceProvider = serviceProvider;
            _services = services;
        }

        public IEventBus Subscribe<TEvent, TEventHandler>()
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            _services.AddScoped<ISubscriber<TEvent>, TEventHandler>();
            return this;
        }

        public IEventBus SubscribeAllHandledEvents<TEventHandler>()
            where TEventHandler : class
        {
            Type implementationType = typeof(TEventHandler);
            IEnumerable<Type> serviceTypes = implementationType
                .GetInterfaces()
                .Where(i => i.IsGenericType)
                .Where(i => i.GetGenericTypeDefinition() == typeof(ISubscriber<>));

            foreach (Type serviceType in serviceTypes)
            {
                _services.AddScoped(serviceType, implementationType);
            }

            return this;
        }

        public async Task PublishAsync<TEvent>(TEvent @event)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                Type eventType = @event.GetType();
                Type openHandlerType = typeof(ISubscriber<>);
                Type handlerType = openHandlerType.MakeGenericType(eventType);
                IEnumerable<object> handlers = scope.ServiceProvider.GetServices(handlerType);
                foreach (object handler in handlers)
                {
                    object result = handlerType
                        .GetTypeInfo()
                        .GetDeclaredMethod(nameof(ISubscriber<TEvent>.HandleAsync))
                        .Invoke(handler, new object[] { @event, CancellationToken.None});
                    await (Task)result;
                }
            }
        }
    }
}
