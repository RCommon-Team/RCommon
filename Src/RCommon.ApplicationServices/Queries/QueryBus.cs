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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.ApplicationServices.Validation;
using RCommon.Caching;
using RCommon.Models.Queries;
using RCommon.Reflection;

namespace RCommon.ApplicationServices.Queries
{
    /// <summary>
    /// Default implementation of <see cref="IQueryBus"/> that dispatches queries to their registered handlers
    /// using the dependency injection container.
    /// </summary>
    /// <remarks>
    /// The query bus resolves the appropriate <see cref="IQueryHandler{TQuery, TResult}"/> from the service provider,
    /// optionally validates the query via <see cref="IValidationService"/>, and invokes the handler using
    /// a dynamically compiled delegate. Compiled handler delegates can optionally be cached for improved performance.
    /// </remarks>
    public class QueryBus : IQueryBus
    {
        /// <summary>
        /// Holds the resolved handler type and the compiled delegate used to invoke it.
        /// </summary>
        private class HandlerFuncMapping
        {
            /// <summary>Gets or sets the closed generic handler interface type for the query.</summary>
            public Type QueryHandlerType { get; set; } = default!;

            /// <summary>Gets or sets the compiled delegate that invokes <c>HandleAsync</c> on the handler.</summary>
            public Func<IQueryHandler, IQuery, CancellationToken, Task> HandlerFunc { get; set; } = default!;
        }

        private readonly ILogger<QueryBus> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IValidationService _validationService;
        private readonly IOptions<CqrsValidationOptions> _validationOptions;
        private readonly CachingOptions _cachingOptions;
        private ICacheService? _cacheService;

        /// <summary>
        /// Initializes a new instance of <see cref="QueryBus"/>.
        /// </summary>
        /// <param name="logger">Logger for tracing query execution.</param>
        /// <param name="serviceProvider">Service provider used to resolve query handlers.</param>
        /// <param name="validationService">Service used to validate queries before execution.</param>
        /// <param name="validationOptions">Options controlling whether query validation is enabled.</param>
        /// <param name="cachingOptions">Options controlling whether dynamically compiled expressions are cached.</param>
        public QueryBus(ILogger<QueryBus> logger, IServiceProvider serviceProvider, IValidationService validationService,
            IOptions<CqrsValidationOptions> validationOptions, IOptions<CachingOptions> cachingOptions)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _validationService = validationService;
            _validationOptions = validationOptions;
            _cachingOptions = cachingOptions.Value;
        }

        /// <inheritdoc />
        public async Task<TResult> DispatchQueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            // Validate the query if validation is configured for queries
            if (_validationOptions.Value != null && _validationOptions.Value.ValidateQueries)
            {
                // TODO: Would be nice to be able to take validation outcome and put in IQuery. Need some casting magic
                await _validationService.ValidateAsync(query, true, cancellationToken);
            }

            var queryType = query.GetType();
            var handlerFunc = GetHandlerFuncMapping(queryType);

            var queryHandler = (IQueryHandler)_serviceProvider.GetRequiredService(handlerFunc.QueryHandlerType);
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(
                    "Executing query {QueryType} ({QueryHandlerType}) by using query handler {QueryHandlerType}",
                    queryType.PrettyPrint(),
                    handlerFunc.QueryHandlerType.PrettyPrint(),
                    queryHandler.GetType().PrettyPrint());
            }

            // Invoke the handler via the dynamically compiled delegate
            var task = (Task<TResult>)handlerFunc.HandlerFunc(queryHandler, query, cancellationToken);

            return await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the <see cref="HandlerFuncMapping"/> for the given query type, optionally retrieving
        /// from cache if expression caching is enabled.
        /// </summary>
        /// <param name="queryType">The runtime type of the query being dispatched.</param>
        /// <returns>The handler function mapping containing the handler type and compiled invoker delegate.</returns>
        private HandlerFuncMapping GetHandlerFuncMapping(Type queryType)
        {
            // When caching is enabled, cache the compiled expression to avoid repeated reflection/compilation
            if (_cachingOptions.CachingEnabled && _cachingOptions.CacheDynamicallyCompiledExpressions)
            {
                var cachingFactory = _serviceProvider.GetService<ICommonFactory<ExpressionCachingStrategy, ICacheService>>();
                Guard.Against<InvalidCacheException>(cachingFactory == null, "We could not properly inject the caching factory: 'ICommonFactory<ExpressionCachingStrategy, ICacheService>>' into the QueryBus");
                _cacheService = cachingFactory!.Create(ExpressionCachingStrategy.Default);
                return _cacheService.GetOrCreate(CacheKey.With(GetType(), queryType.GetCacheKey()),
                    () => this.BuildHandlerFuncMapping(queryType));
            }
            return this.BuildHandlerFuncMapping(queryType);

        }

        /// <summary>
        /// Builds the <see cref="HandlerFuncMapping"/> by reflecting over the query type to determine
        /// the handler interface and compiling a delegate for <c>HandleAsync</c>.
        /// </summary>
        /// <param name="queryType">The runtime type of the query being dispatched.</param>
        /// <returns>A new <see cref="HandlerFuncMapping"/> instance.</returns>
        private HandlerFuncMapping BuildHandlerFuncMapping(Type queryType)
        {
            // Find the IQuery<TResult> interface to extract the result type
            var queryInterfaceType = queryType
                        .GetTypeInfo()
                        .GetInterfaces()
                        .Single(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));

            // Construct the closed generic IQueryHandler<TQuery, TResult> type
            var queryHandlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, queryInterfaceType.GetTypeInfo().GetGenericArguments()[0]);

            // Compile a strongly-typed delegate for the handler's HandleAsync method
            var invokeExecuteQueryAsync = ReflectionHelper.CompileMethodInvocation<Func<IQueryHandler, IQuery, CancellationToken, Task>>(
                queryHandlerType,
                "HandleAsync",
                queryType, typeof(CancellationToken));
            return new HandlerFuncMapping
            {
                QueryHandlerType = queryHandlerType,
                HandlerFunc = invokeExecuteQueryAsync
            };
        }
    }
}
