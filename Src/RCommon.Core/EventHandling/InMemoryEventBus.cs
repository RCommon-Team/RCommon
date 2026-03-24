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
    /// Subscriptions registered via <see cref="Subscribe{TEvent, TEventHandler}"/> or
    /// <see cref="SubscribeAllHandledEvents{TEventHandler}"/> are tracked internally and resolved
    /// at publish time via <see cref="ActivatorUtilities"/>. For best results, register subscribers
    /// in the DI container during configuration using <c>InMemoryEventBusBuilderExtensions.AddSubscriber</c>.
    /// </remarks>
    public class InMemoryEventBus : IEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentBag<(Type serviceType, Type implementationType)> _dynamicSubscriptions = new();

        /// <summary>
        /// Initializes a new instance of <see cref="InMemoryEventBus"/>.
        /// </summary>
        /// <param name="serviceProvider">The root service provider used to create scopes for event publishing.</param>
        public InMemoryEventBus(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Tracks the subscription internally. The handler will be resolved via <see cref="ActivatorUtilities"/>
        /// at publish time within a new scope.
        /// </remarks>
        public IEventBus Subscribe<TEvent, TEventHandler>()
            where TEvent : class
            where TEventHandler : class, ISubscriber<TEvent>
        {
            _dynamicSubscriptions.Add((typeof(ISubscriber<TEvent>), typeof(TEventHandler)));
            return this;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Uses reflection to discover all <see cref="ISubscriber{TEvent}"/> interfaces on
        /// <typeparamref name="TEventHandler"/> and tracks each for resolution at publish time.
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
                _dynamicSubscriptions.Add((serviceType, implementationType));
            }

            return this;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Creates a new DI scope and uses reflection to resolve handlers for the runtime event type,
        /// invoking <see cref="ISubscriber{TEvent}.HandleAsync"/> on each handler sequentially.
        /// Also resolves handlers from dynamic subscriptions registered via <see cref="Subscribe{TEvent, TEventHandler}"/>.
        /// </remarks>
        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                // Resolve handlers based on the runtime event type (not the compile-time generic parameter)
                Type eventType = @event!.GetType();
                Type openHandlerType = typeof(ISubscriber<>);
                Type handlerType = openHandlerType.MakeGenericType(eventType);
                IEnumerable<object?> handlers = scope.ServiceProvider.GetServices(handlerType);

                // Also resolve dynamically subscribed handlers via ActivatorUtilities
                var dynamicHandlers = _dynamicSubscriptions
                    .Where(s => s.serviceType == handlerType)
                    .Select(s => ActivatorUtilities.CreateInstance(scope.ServiceProvider, s.implementationType));

                foreach (object? handler in handlers.Concat(dynamicHandlers))
                {
                    if (handler == null) continue;

                    // Invoke HandleAsync via reflection to support polymorphic dispatch
                    object? result = handlerType
                        .GetTypeInfo()
                        .GetDeclaredMethod(nameof(ISubscriber<TEvent>.HandleAsync))
                        ?.Invoke(handler, new object[] { @event, cancellationToken });
                    if (result is Task task)
                    {
                        await task.ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
