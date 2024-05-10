using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.EventHandling.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling
{
    public static class InMemoryEventBusBuilderExtensions
    {
        public static void AddSubscriber<TEvent, TEventHandler>(this IInMemoryEventBusBuilder builder)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.AddScoped<ISubscriber<TEvent>, TEventHandler>();
        }

        public static void AddSubscriber<TEvent, TEventHandler>(this IInMemoryEventBusBuilder builder, Func<IServiceProvider, TEventHandler> getSubscriber)
            where TEvent : class, ISerializableEvent
            where TEventHandler : class, ISubscriber<TEvent>
        {
            builder.Services.TryAddScoped(getSubscriber);
        }

        public static void AddSubscribers(this IInMemoryEventBusBuilder builder, params Type[] queryHandlerTypes)
        {
            AddSubscribers(builder, (IEnumerable<Type>)queryHandlerTypes);
        }

        public static void AddSubscribers(this IInMemoryEventBusBuilder builder, Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);
            var subscribeSynchronousToTypes = fromAssembly
                .GetTypes()
                .Where(t => t.GetTypeInfo().GetInterfaces().Any(IsSubscriberInterface))
                .Where(t => !t.HasConstructorParameterOfType(IsSubscriberInterface))
                .Where(t => predicate(t));
            AddSubscribers(builder, subscribeSynchronousToTypes);
        }

        public static void AddSubscribers(this IInMemoryEventBusBuilder builder, IEnumerable<Type> queryHandlerTypes)
        {
            foreach (var queryHandlerType in queryHandlerTypes)
            {
                var t = queryHandlerType;
                if (t.GetTypeInfo().IsAbstract) continue;
                var queryHandlerInterfaces = t
                    .GetTypeInfo()
                    .GetInterfaces()
                    .Where(IsSubscriberInterface)
                    .ToList();
                if (!queryHandlerInterfaces.Any())
                {
                    throw new ArgumentException($"Type '{t.PrettyPrint()}' is not an '{typeof(ISubscriber<>).PrettyPrint()}'");
                }

                foreach (var queryHandlerInterface in queryHandlerInterfaces)
                {
                    builder.Services.AddTransient(queryHandlerInterface, t);
                }
            }
        }

        private static bool IsSubscriberInterface(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(ISubscriber<>);
        }
    }
}
