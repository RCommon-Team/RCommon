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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.ApplicationServices.Validation;
using RCommon.Caching;
using RCommon.Models.Commands;
using RCommon.Models.ExecutionResults;
using RCommon.Reflection;

namespace RCommon.ApplicationServices.Commands
{
    public class CommandBus : ICommandBus
    {
        private readonly ILogger<CommandBus> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IValidationService _validationService;
        private readonly IOptions<CqrsValidationOptions> _validationOptions;
        private readonly ICacheService _cacheService;
        private readonly CachingOptions _cachingOptions;

        public CommandBus(ILogger<CommandBus> logger, IServiceProvider serviceProvider, IValidationService validationService, 
            IOptions<CqrsValidationOptions> validationOptions, IOptions<CachingOptions> cachingOptions, ICommonFactory<ExpressionCachingStrategy, ICacheService> cacheFactory)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _validationService = validationService;
            _validationOptions = validationOptions;
            _cacheService = cacheFactory.Create(ExpressionCachingStrategy.Default);
            _cachingOptions = cachingOptions.Value;
        }

        public async Task<TResult> DispatchCommandAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
            where TResult : IExecutionResult
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            if (_validationOptions.Value != null && _validationOptions.Value.ValidateCommands)
            {
                // TODO: Would be nice to be able to take validation outcome and put in FailedExecutionResult. Need some casting magic
                await _validationService.ValidateAsync(command, true, cancellationToken);
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(
                    "Executing command {CommandType}",
                    command.GetType().PrettyPrint());
            }

            var commandResult = await ExecuteHandlerAsync(command, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(
                        "Execution command {CommandType} was success: {IsSuccess}",
                        command.GetType().PrettyPrint(),
                        commandResult?.IsSuccess);
            }

            return commandResult;
        }

        private async Task<TResult> ExecuteHandlerAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
            where TResult : IExecutionResult
        {
            var commandType = command.GetType();
            var commandExecutionDetails = GetCommandExecutionDetails(commandType);

            var commandHandlers = _serviceProvider.GetServices(commandExecutionDetails.CommandHandlerType)
                .Cast<ICommandHandler>()
                .ToList();
            if (!commandHandlers.Any())
            {
                throw new NoCommandHandlersException(string.Format(
                    "No command handlers registered for the command '{0}'",
                    commandType.PrettyPrint()));
            }
            if (commandHandlers.Count > 1)
            {
                throw new InvalidOperationException(string.Format(
                    "Too many command handlers the command '{0}'. These were found: {1}",
                    commandType.PrettyPrint(),
                    string.Join(", ", commandHandlers.Select(h => h.GetType().PrettyPrint()))));
            }

            var commandHandler = commandHandlers.Single();

            var task =  (Task<TResult>)commandExecutionDetails.Invoker(commandHandler, command, cancellationToken);
            return await task;
        }

        private class CommandExecutionDetails
        {
            public Type CommandHandlerType { get; set; }
            public Func<ICommandHandler, ICommand, CancellationToken, Task> Invoker { get; set; }
        }

        private const string NameOfExecuteCommand = nameof(
            ICommandHandler<
                IExecutionResult,
                ICommand<IExecutionResult>
            >.HandleAsync);
        private CommandExecutionDetails GetCommandExecutionDetails(Type commandType)
        {
            if (_cachingOptions.CachingEnabled && _cachingOptions.CacheDynamicallyCompiledExpressions)
            {
                return _cacheService.GetOrCreate(CacheKey.With(GetType(), commandType.GetCacheKey()), () => this.BuildCommandDetails(commandType));
            }
            return this.BuildCommandDetails(commandType);
        }

        private CommandExecutionDetails BuildCommandDetails(Type commandType)
        {
            var commandInterfaceType = commandType
                        .GetTypeInfo()
                        .GetInterfaces()
                        .Single(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
            var commandTypes = commandInterfaceType.GetTypeInfo().GetGenericArguments();

            var commandHandlerType = typeof(ICommandHandler<,>)
                .MakeGenericType(commandTypes[0], commandType);

            _logger.LogDebug(
                "Command {CommandType} is resolved by {CommandHandlerType}",
                commandType.PrettyPrint(),
                commandHandlerType.PrettyPrint());

            var invokeExecuteAsync = ReflectionHelper.CompileMethodInvocation<Func<ICommandHandler, ICommand, CancellationToken, Task>>(
                commandHandlerType, NameOfExecuteCommand);

            return new CommandExecutionDetails
            {
                CommandHandlerType = commandHandlerType,
                Invoker = invokeExecuteAsync
            };
        }
    }
}
