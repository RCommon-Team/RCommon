using Microsoft.Extensions.DependencyInjection;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.Commands;
using RCommon.ApplicationServices.ExecutionResults;
using RCommon.ApplicationServices.Queries;
using System;
using System.Collections.Generic;
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

        public static void AddCommandHandler<TCommandHandler, TCommand, TResult>(this ICqrsBuilder builder)
           where TCommandHandler : class, ICommandHandler<TResult, TCommand>
           where TCommand : ICommand<TResult>
            where TResult : IExecutionResult
        {
            builder.Services.AddTransient<ICommandHandler<TResult, TCommand>, TCommandHandler>();
        }
    }
}
