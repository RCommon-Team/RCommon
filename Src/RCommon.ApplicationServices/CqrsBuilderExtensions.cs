// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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


using Microsoft.Extensions.DependencyInjection;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.Commands;
using RCommon.ApplicationServices.Queries;
using RCommon.Models.Commands;
using RCommon.Models.ExecutionResults;
using RCommon.Models.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace RCommon
{
    /// <summary>
    /// Extension methods for <see cref="IRCommonBuilder"/> and <see cref="ICqrsBuilder"/> that provide
    /// fluent registration of CQRS infrastructure, command handlers, and query handlers.
    /// </summary>
    public static class CqrsBuilderExtensions
    {
        /// <summary>
        /// Adds CQRS support using the specified <see cref="ICqrsBuilder"/> implementation with default configuration.
        /// </summary>
        /// <typeparam name="T">The <see cref="ICqrsBuilder"/> implementation type to use.</typeparam>
        /// <param name="builder">The RCommon builder.</param>
        /// <returns>The <paramref name="builder"/> for further chaining.</returns>
        public static IRCommonBuilder WithCQRS<T>(this IRCommonBuilder builder)
            where T : ICqrsBuilder
        {

            return WithCQRS<T>(builder, x => { });
        }

        /// <summary>
        /// Adds CQRS support using the specified <see cref="ICqrsBuilder"/> implementation and applies additional configuration.
        /// </summary>
        /// <typeparam name="T">The <see cref="ICqrsBuilder"/> implementation type to use.</typeparam>
        /// <param name="builder">The RCommon builder.</param>
        /// <param name="actions">A delegate to configure the CQRS builder (e.g., register handlers).</param>
        /// <returns>The <paramref name="builder"/> for further chaining.</returns>
        public static IRCommonBuilder WithCQRS<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : ICqrsBuilder
        {

            // Instantiate the CQRS builder implementation, which registers core bus services in its constructor
            var cqrsBuilder = (T)Activator.CreateInstance(typeof(T), new object[] { builder })!;
            actions(cqrsBuilder);
            return builder;
        }

        /// <summary>
        /// Registers a query handler as a transient service for the specified query and result types.
        /// </summary>
        /// <typeparam name="TQueryHandler">The query handler implementation type.</typeparam>
        /// <typeparam name="TQuery">The query type.</typeparam>
        /// <typeparam name="TResult">The query result type.</typeparam>
        /// <param name="builder">The CQRS builder.</param>
        public static void AddQueryHandler<TQueryHandler, TQuery, TResult>(this ICqrsBuilder builder)
           where TQueryHandler : class, IQueryHandler<TQuery, TResult>
           where TQuery : IQuery<TResult>
        {
            builder.Services.AddTransient<IQueryHandler<TQuery, TResult>, TQueryHandler>();
        }

        /// <summary>
        /// Registers a query handler as a transient service. This is an alias for <see cref="AddQueryHandler{TQueryHandler, TQuery, TResult}"/>
        /// with a query-first type parameter order.
        /// </summary>
        /// <typeparam name="TQuery">The query type.</typeparam>
        /// <typeparam name="TQueryHandler">The query handler implementation type.</typeparam>
        /// <typeparam name="TResult">The query result type.</typeparam>
        /// <param name="builder">The CQRS builder.</param>
        public static void AddQuery<TQuery, TQueryHandler, TResult>(this ICqrsBuilder builder)
           where TQueryHandler : class, IQueryHandler<TQuery, TResult>
           where TQuery : IQuery<TResult>
        {
            builder.Services.AddTransient<IQueryHandler<TQuery, TResult>, TQueryHandler>();
        }

        /// <summary>
        /// Registers a command handler as a transient service for the specified command and result types.
        /// </summary>
        /// <typeparam name="TCommandHandler">The command handler implementation type.</typeparam>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <typeparam name="TResult">The execution result type.</typeparam>
        /// <param name="builder">The CQRS builder.</param>
        public static void AddCommandHandler<TCommandHandler, TCommand, TResult>(this ICqrsBuilder builder)
           where TCommandHandler : class, ICommandHandler<TResult, TCommand>
           where TCommand : ICommand<TResult>
            where TResult : IExecutionResult
        {
            builder.Services.AddTransient<ICommandHandler<TResult, TCommand>, TCommandHandler>();
        }

        /// <summary>
        /// Registers a command handler as a transient service. This is an alias for <see cref="AddCommandHandler{TCommandHandler, TCommand, TResult}"/>
        /// with a command-first type parameter order.
        /// </summary>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <typeparam name="TCommandHandler">The command handler implementation type.</typeparam>
        /// <typeparam name="TResult">The execution result type.</typeparam>
        /// <param name="builder">The CQRS builder.</param>
        public static void AddCommand<TCommand, TCommandHandler, TResult>(this ICqrsBuilder builder)
           where TCommandHandler : class, ICommandHandler<TResult, TCommand>
           where TCommand : ICommand<TResult>
            where TResult : IExecutionResult
        {
            builder.Services.AddTransient<ICommandHandler<TResult, TCommand>, TCommandHandler>();
        }

        /// <summary>
        /// Scans the specified assembly for <see cref="ICommandHandler{TResult, TCommand}"/> implementations
        /// and registers them as transient services.
        /// </summary>
        /// <param name="builder">The CQRS builder.</param>
        /// <param name="fromAssembly">The assembly to scan for command handler types.</param>
        /// <param name="predicate">An optional predicate to filter which handler types to register.</param>
        /// <remarks>
        /// Types whose constructors accept an <see cref="ICommandHandler{TResult, TCommand}"/> parameter are excluded
        /// to avoid registering decorator types as base handlers.
        /// </remarks>
        public static void AddCommandHandlers(this ICqrsBuilder builder, Assembly fromAssembly, Predicate<Type>? predicate = null)
        {
            predicate = predicate ?? (t => true);
            var commandHandlerTypes = fromAssembly
                .GetTypes()
                .Where(t => t.GetTypeInfo().GetInterfaces().Any(IsCommandHandlerInterface))
                // Exclude types that accept a command handler in their constructor (likely decorators)
                .Where(t => !t.HasConstructorParameterOfType(IsCommandHandlerInterface))
                .Where(t => predicate(t));
            AddCommandHandlers(builder, commandHandlerTypes);
        }

        /// <summary>
        /// Registers the specified command handler types as transient services.
        /// </summary>
        /// <param name="builder">The CQRS builder.</param>
        /// <param name="commandHandlerTypes">The command handler types to register.</param>
        public static void AddCommandHandlers(this ICqrsBuilder builder, params Type[] commandHandlerTypes)
        {
            AddCommandHandlers(builder, (IEnumerable<Type>)commandHandlerTypes);
        }

        /// <summary>
        /// Registers the specified command handler types as transient services.
        /// </summary>
        /// <param name="builder">The CQRS builder.</param>
        /// <param name="commandHandlerTypes">The command handler types to register.</param>
        /// <exception cref="ArgumentException">Thrown when a type does not implement <see cref="ICommandHandler{TResult, TCommand}"/>.</exception>
        public static void AddCommandHandlers(this ICqrsBuilder builder, IEnumerable<Type> commandHandlerTypes)
        {
            foreach (var commandHandlerType in commandHandlerTypes)
            {
                var t = commandHandlerType;
                // Skip abstract types as they cannot be instantiated
                if (t.GetTypeInfo().IsAbstract) continue;
                var handlesCommandTypes = t
                    .GetTypeInfo()
                    .GetInterfaces()
                    .Where(IsCommandHandlerInterface)
                    .ToList();
                if (!handlesCommandTypes.Any())
                {
                    throw new ArgumentException($"Type '{commandHandlerType.PrettyPrint()}' does not implement '{typeof(ICommandHandler<,>).PrettyPrint()}'");
                }

                // Register the handler type for each command handler interface it implements
                foreach (var handlesCommandType in handlesCommandTypes)
                {
                    builder.Services.AddTransient(handlesCommandType, t);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified type is a closed generic form of <see cref="ICommandHandler{TResult, TCommand}"/>.
        /// </summary>
        private static bool IsCommandHandlerInterface(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(ICommandHandler<,>);
        }

        /// <summary>
        /// Registers the specified query handler types as transient services.
        /// </summary>
        /// <param name="builder">The CQRS builder.</param>
        /// <param name="queryHandlerTypes">The query handler types to register.</param>
        public static void AddQueryHandlers(this ICqrsBuilder builder, params Type[] queryHandlerTypes)
        {
            AddQueryHandlers(builder, (IEnumerable<Type>)queryHandlerTypes);
        }

        /// <summary>
        /// Scans the specified assembly for <see cref="IQueryHandler{TQuery, TResult}"/> implementations
        /// and registers them as transient services.
        /// </summary>
        /// <param name="builder">The CQRS builder.</param>
        /// <param name="fromAssembly">The assembly to scan for query handler types.</param>
        /// <param name="predicate">An optional predicate to filter which handler types to register.</param>
        /// <remarks>
        /// Types whose constructors accept an <see cref="IQueryHandler{TQuery, TResult}"/> parameter are excluded
        /// to avoid registering decorator types as base handlers.
        /// </remarks>
        public static void AddQueryHandlers(this ICqrsBuilder builder, Assembly fromAssembly,
            Predicate<Type>? predicate = null)
        {
            predicate = predicate ?? (t => true);
            var subscribeSynchronousToTypes = fromAssembly
                .GetTypes()
                .Where(t => t.GetTypeInfo().GetInterfaces().Any(IsQueryHandlerInterface))
                // Exclude types that accept a query handler in their constructor (likely decorators)
                .Where(t => !t.HasConstructorParameterOfType(IsQueryHandlerInterface))
                .Where(t => predicate(t));
            AddQueryHandlers(builder, subscribeSynchronousToTypes);
        }

        /// <summary>
        /// Registers the specified query handler types as transient services.
        /// </summary>
        /// <param name="builder">The CQRS builder.</param>
        /// <param name="queryHandlerTypes">The query handler types to register.</param>
        /// <exception cref="ArgumentException">Thrown when a type does not implement <see cref="IQueryHandler{TQuery, TResult}"/>.</exception>
        public static void AddQueryHandlers(this ICqrsBuilder builder, IEnumerable<Type> queryHandlerTypes)
        {
            foreach (var queryHandlerType in queryHandlerTypes)
            {
                var t = queryHandlerType;
                // Skip abstract types as they cannot be instantiated
                if (t.GetTypeInfo().IsAbstract) continue;
                var queryHandlerInterfaces = t
                    .GetTypeInfo()
                    .GetInterfaces()
                    .Where(IsQueryHandlerInterface)
                    .ToList();
                if (!queryHandlerInterfaces.Any())
                {
                    throw new ArgumentException($"Type '{t.PrettyPrint()}' is not an '{typeof(IQueryHandler<,>).PrettyPrint()}'");
                }

                // Register the handler type for each query handler interface it implements
                foreach (var queryHandlerInterface in queryHandlerInterfaces)
                {
                    builder.Services.AddTransient(queryHandlerInterface, t);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified type is a closed generic form of <see cref="IQueryHandler{TQuery, TResult}"/>.
        /// </summary>
        private static bool IsQueryHandlerInterface(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>);
        }
    }
}
