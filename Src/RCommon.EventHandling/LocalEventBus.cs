// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
// https://github.com/eventflow/EventFlow
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

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.EventHandling.Subscribers;
using RCommon.Reflection;

namespace RCommon.EventHandling
{
    public class LocalEventBus : IEventBus
    {
        private class CacheItem
        {
            public Type EventHandlerType { get; set; }
            public Func<ILocalEventHandler, ILocalEvent, CancellationToken, Task> HandlerFunc { get; set; }
        }

        private readonly ILogger<LocalEventBus> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _memoryCache;

        public LocalEventBus(ILogger<LocalEventBus> logger, IServiceProvider serviceProvider, IMemoryCache memoryCache)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _memoryCache = memoryCache;
        }

        public async Task DispatchEventAsync<TResult>(ILocalEvent query, CancellationToken cancellationToken)
        {
            var eventType = query.GetType();
            var cacheItem = GetCacheItem(eventType);

            var eventHandler = (ILocalEventHandler)_serviceProvider.GetRequiredService(cacheItem.EventHandlerType);
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(
                    "Dispatching event {EventType} ({EventHandlerType}) by using local event handler {EventHandlerType}",
                    eventType.PrettyPrint(),
                    cacheItem.EventHandlerType.PrettyPrint(),
                    eventHandler.GetType().PrettyPrint());
            }

            var task = (Task<TResult>)cacheItem.HandlerFunc(eventHandler, query, cancellationToken);

            await task.ConfigureAwait(false);
        }

        private CacheItem GetCacheItem(Type queryType)
        {
            return _memoryCache.GetOrCreate(
                CacheKey.With(GetType(), queryType.GetCacheKey()),
                e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
                    var queryInterfaceType = queryType
                        .GetTypeInfo()
                        .GetInterfaces()
                        .Single(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(ILocalEvent));
                    var eventHandlerType = typeof(ILocalEventHandler<>).MakeGenericType(queryType, queryInterfaceType.GetTypeInfo().GetGenericArguments()[0]);
                    var invokeDispatchEventAsync = ReflectionHelper.CompileMethodInvocation<Func<ILocalEventHandler, ILocalEvent, CancellationToken, Task>>(
                        eventHandlerType,
                        "DispatchEventAsync",
                        queryType, typeof(CancellationToken));
                    return new CacheItem
                    {
                        EventHandlerType = eventHandlerType,
                        HandlerFunc = invokeDispatchEventAsync
                    };
                });
        }
    }
}
