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
    /// <summary>
    /// Default implementation of <see cref="ICommandBus"/> that dispatches commands to their registered handlers
    /// using the dependency injection container.
    /// </summary>
    /// <remarks>
    /// The command bus resolves the appropriate <see cref="ICommandHandler{TResult, TCommand}"/> from the service provider,
    /// optionally validates the command via <see cref="IValidationService"/>, and invokes the handler using
    /// a dynamically compiled delegate. Compiled handler delegates can optionally be cached for improved performance.
    /// </remarks>
    public class CommandBus : ICommandBus
    {
        private readonly ILogger<CommandBus> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IValidationService _validationService;
        private readonly IOptions<CqrsValidationOptions> _validationOptions;
        private ICacheService? _cacheService;
        private readonly CachingOptions _cachingOptions;

        /// <summary>
        /// Initializes a new instance of <see cref="CommandBus"/>.
        /// </summary>
        /// <param name="logger">Logger for tracing command execution.</param>
        /// <param name="serviceProvider">Service provider used to resolve command handlers.</param>
        /// <param name="validationService">Service used to validate commands before execution.</param>
        /// <param name="validationOptions">Options controlling whether command validation is enabled.</param>
        /// <param name="cachingOptions">Options controlling whether dynamically compiled expressions are cached.</param>
        public CommandBus(ILogger<CommandBus> logger, IServiceProvider serviceProvider, IValidationService validationService,
            IOptions<CqrsValidationOptions> validationOptions, IOptions<CachingOptions> cachingOptions)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _validationService = validationService;
            _validationOptions = validationOptions;
            _cachingOptions = cachingOptions.Value;
        }

        /// <inheritdoc />
        public async Task<TResult> DispatchCommandAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
            where TResult : IExecutionResult
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            // Validate the command if validation is configured for commands
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

            return commandResult!;
        }

        /// <summary>
        /// Resolves and invokes the single registered handler for the given command.
        /// </summary>
        /// <typeparam name="TResult">The execution result type returned by the handler.</typeparam>
        /// <param name="command">The command to execute.</param>
        /// <param name="cancellationToken">Token to observe for cancellation.</param>
        /// <returns>The result produced by the command handler.</returns>
        /// <exception cref="NoCommandHandlersException">Thrown when no handler is registered for the command type.</exception>
        /// <exception cref="InvalidOperationException">Thrown when more than one handler is registered for the command type.</exception>
        private async Task<TResult> ExecuteHandlerAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
            where TResult : IExecutionResult
        {
            var commandType = command.GetType();
            var commandExecutionDetails = GetCommandExecutionDetails(commandType);

            // Resolve all registered handlers for this command type and enforce exactly one
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

            // Invoke the handler via the dynamically compiled delegate
            var task =  (Task<TResult>)commandExecutionDetails.Invoker(commandHandler, command, cancellationToken);
            return await task;
        }

        /// <summary>
        /// Holds the resolved handler type and the compiled delegate used to invoke it.
        /// </summary>
        private class CommandExecutionDetails
        {
            /// <summary>Gets or sets the closed generic handler interface type for the command.</summary>
            public Type CommandHandlerType { get; set; } = default!;

            /// <summary>Gets or sets the compiled delegate that invokes <c>HandleAsync</c> on the handler.</summary>
            public Func<ICommandHandler, ICommand, CancellationToken, Task> Invoker { get; set; } = default!;
        }

        private const string NameOfExecuteCommand = nameof(
            ICommandHandler<
                IExecutionResult,
                ICommand<IExecutionResult>
            >.HandleAsync);

        /// <summary>
        /// Gets the <see cref="CommandExecutionDetails"/> for the given command type, optionally retrieving
        /// from cache if expression caching is enabled.
        /// </summary>
        /// <param name="commandType">The runtime type of the command being dispatched.</param>
        /// <returns>The execution details containing the handler type and compiled invoker delegate.</returns>
        private CommandExecutionDetails GetCommandExecutionDetails(Type commandType)
        {
            // When caching is enabled, cache the compiled expression to avoid repeated reflection/compilation
            if (_cachingOptions.CachingEnabled && _cachingOptions.CacheDynamicallyCompiledExpressions)
            {
                var cachingFactory = _serviceProvider.GetService<ICommonFactory<ExpressionCachingStrategy, ICacheService>>();
                Guard.Against<InvalidCacheException>(cachingFactory == null, "We could not properly inject the caching factory: 'ICommonFactory<ExpressionCachingStrategy, ICacheService>>' into the CommandBus");
                _cacheService = cachingFactory!.Create(ExpressionCachingStrategy.Default);
                return _cacheService.GetOrCreate(CacheKey.With(GetType(), commandType.GetCacheKey()), () => this.BuildCommandDetails(commandType));
            }
            return this.BuildCommandDetails(commandType);
        }

        /// <summary>
        /// Builds the <see cref="CommandExecutionDetails"/> by reflecting over the command type to determine
        /// the handler interface and compiling a delegate for <c>HandleAsync</c>.
        /// </summary>
        /// <param name="commandType">The runtime type of the command being dispatched.</param>
        /// <returns>A new <see cref="CommandExecutionDetails"/> instance.</returns>
        private CommandExecutionDetails BuildCommandDetails(Type commandType)
        {
            // Find the ICommand<TResult> interface to extract the result type
            var commandInterfaceType = commandType
                        .GetTypeInfo()
                        .GetInterfaces()
                        .Single(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
            var commandTypes = commandInterfaceType.GetTypeInfo().GetGenericArguments();

            // Construct the closed generic ICommandHandler<TResult, TCommand> type
            var commandHandlerType = typeof(ICommandHandler<,>)
                .MakeGenericType(commandTypes[0], commandType);

            _logger.LogDebug(
                "Command {CommandType} is resolved by {CommandHandlerType}",
                commandType.PrettyPrint(),
                commandHandlerType.PrettyPrint());

            // Compile a strongly-typed delegate for the handler's HandleAsync method
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
