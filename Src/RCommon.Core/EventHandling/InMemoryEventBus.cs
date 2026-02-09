#region MIT License
// The MIT License (MIT)
// 
// Original Source: https://github.com/jacqueskang/EventBus/blob/develop/src/JKang.EventBus.Core/InMemory/InMemoryEventBus.cs
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion

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
    /// <summary>
    /// In-memory implementation of <see cref="IEventBus"/> that resolves and invokes
    /// <see cref="ISubscriber{TEvent}"/> handlers from the dependency injection container.
    /// </summary>
    /// <remarks>
    /// Subscriptions are registered directly into the <see cref="IServiceCollection"/> at configuration time.
    /// Publishing creates a new scope and resolves all handlers via reflection to support polymorphic event dispatch.
    /// </remarks>
    public class InMemoryEventBus : IEventBus
    {
        private readonly IServiceCollection _services;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="InMemoryEventBus"/>.
        /// </summary>
        /// <param name="serviceProvider">The root service provider used to create scopes for event publishing.</param>
        /// <param name="services">The service collection for registering subscriber services at configuration time.</param>
        public InMemoryEventBus(IServiceProvider serviceProvider, IServiceCollection services)
        {
            _serviceProvider = serviceProvider;
            _services = services;
        }

        /// <inheritdoc />
        public IEventBus Subscribe<TEvent, TEventHandler>()
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            _services.AddScoped<ISubscriber<TEvent>, TEventHandler>();
            return this;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Uses reflection to discover all <see cref="ISubscriber{TEvent}"/> interfaces on
        /// <typeparamref name="TEventHandler"/> and registers each as a scoped service.
        /// </remarks>
        public IEventBus SubscribeAllHandledEvents<TEventHandler>()
            where TEventHandler : class
        {
            Type implementationType = typeof(TEventHandler);
            // Discover all ISubscriber<> interfaces implemented by the handler type
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

        /// <inheritdoc />
        /// <remarks>
        /// Creates a new DI scope and uses reflection to resolve handlers for the runtime event type,
        /// invoking <see cref="ISubscriber{TEvent}.HandleAsync"/> on each handler sequentially.
        /// </remarks>
        public async Task PublishAsync<TEvent>(TEvent @event)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                // Resolve handlers based on the runtime event type (not the compile-time generic parameter)
                Type eventType = @event!.GetType();
                Type openHandlerType = typeof(ISubscriber<>);
                Type handlerType = openHandlerType.MakeGenericType(eventType);
                IEnumerable<object?> handlers = scope.ServiceProvider.GetServices(handlerType);
                foreach (object? handler in handlers)
                {
                    if (handler == null) continue;

                    // Invoke HandleAsync via reflection to support polymorphic dispatch
                    object? result = handlerType
                        .GetTypeInfo()
                        .GetDeclaredMethod(nameof(ISubscriber<TEvent>.HandleAsync))
                        ?.Invoke(handler, new object[] { @event, CancellationToken.None});
                    if (result is Task task)
                    {
                        await task;
                    }
                }
            }
        }
    }
}
