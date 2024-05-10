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
using RCommon.ApplicationServices.ExecutionResults;
using RCommon.ApplicationServices.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace RCommon
{
    public static class CqrsBuilderExtensions
    {
        public static IRCommonBuilder WithCQRS<T>(this IRCommonBuilder builder)
            where T : ICqrsBuilder
        {

            return WithCQRS<T>(builder, x => { });
        }

        public static IRCommonBuilder WithCQRS<T>(this IRCommonBuilder builder, Action<T> actions)
            where T : ICqrsBuilder
        {

            // Event Handling Configurations 
            var cqrsBuilder = (T)Activator.CreateInstance(typeof(T), new object[] { builder });
            actions(cqrsBuilder);
            return builder;
        }

        public static void AddQueryHandler<TQueryHandler, TQuery, TResult>(this ICqrsBuilder builder)
           where TQueryHandler : class, IQueryHandler<TQuery, TResult>
           where TQuery : IQuery<TResult>
        {
            builder.Services.AddTransient<IQueryHandler<TQuery, TResult>, TQueryHandler>();
        }

        public static void AddQuery<TQuery, TQueryHandler, TResult>(this ICqrsBuilder builder)
           where TQueryHandler : class, IQueryHandler<TQuery, TResult>
           where TQuery : IQuery<TResult>
        {
            builder.Services.AddTransient<IQueryHandler<TQuery, TResult>, TQueryHandler>();
        }

        public static void AddCommandHandler<TCommandHandler, TCommand, TResult>(this ICqrsBuilder builder)
           where TCommandHandler : class, ICommandHandler<TResult, TCommand>
           where TCommand : ICommand<TResult>
            where TResult : IExecutionResult
        {
            builder.Services.AddTransient<ICommandHandler<TResult, TCommand>, TCommandHandler>();
        }

        public static void AddCommand<TCommand, TCommandHandler, TResult>(this ICqrsBuilder builder)
           where TCommandHandler : class, ICommandHandler<TResult, TCommand>
           where TCommand : ICommand<TResult>
            where TResult : IExecutionResult
        {
            builder.Services.AddTransient<ICommandHandler<TResult, TCommand>, TCommandHandler>();
        }

        public static void AddCommandHandlers(this ICqrsBuilder builder, Assembly fromAssembly, Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);
            var commandHandlerTypes = fromAssembly
                .GetTypes()
                .Where(t => t.GetTypeInfo().GetInterfaces().Any(IsCommandHandlerInterface))
                .Where(t => !t.HasConstructorParameterOfType(IsCommandHandlerInterface))
                .Where(t => predicate(t));
            AddCommandHandlers(builder, commandHandlerTypes);
        }

        public static void AddCommandHandlers(this ICqrsBuilder builder, params Type[] commandHandlerTypes)
        {
            AddCommandHandlers(builder, (IEnumerable<Type>)commandHandlerTypes);
        }

        public static void AddCommandHandlers(this ICqrsBuilder builder, IEnumerable<Type> commandHandlerTypes)
        {
            foreach (var commandHandlerType in commandHandlerTypes)
            {
                var t = commandHandlerType;
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

                foreach (var handlesCommandType in handlesCommandTypes)
                {
                    builder.Services.AddTransient(handlesCommandType, t);
                }
            }
        }

        private static bool IsCommandHandlerInterface(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(ICommandHandler<,>);
        }

        public static void AddQueryHandlers(this ICqrsBuilder builder, params Type[] queryHandlerTypes)
        {
            AddQueryHandlers(builder, (IEnumerable<Type>)queryHandlerTypes);
        }

        public static void AddQueryHandlers(this ICqrsBuilder builder, Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);
            var subscribeSynchronousToTypes = fromAssembly
                .GetTypes()
                .Where(t => t.GetTypeInfo().GetInterfaces().Any(IsQueryHandlerInterface))
                .Where(t => !t.HasConstructorParameterOfType(IsQueryHandlerInterface))
                .Where(t => predicate(t));
            AddQueryHandlers(builder, subscribeSynchronousToTypes);
        }

        public static void AddQueryHandlers(this ICqrsBuilder builder, IEnumerable<Type> queryHandlerTypes)
        {
            foreach (var queryHandlerType in queryHandlerTypes)
            {
                var t = queryHandlerType;
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

                foreach (var queryHandlerInterface in queryHandlerInterfaces)
                {
                    builder.Services.AddTransient(queryHandlerInterface, t);
                }
            }
        }

        private static bool IsQueryHandlerInterface(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>);
        }
    }
}
