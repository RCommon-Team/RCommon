using RCommon.Models.Commands;
using RCommon.Models.ExecutionResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Commands
{
    /// <summary>
    /// Non-generic marker interface for all command handlers. Used for service resolution via reflection.
    /// </summary>
    public interface ICommandHandler
    {
    }

    /// <summary>Handles commands of specified type.</summary>
    /// <typeparam name="TCommand">Handled command type.</typeparam>
    public interface ICommandHandler<in TCommand> : ICommandHandler
         where TCommand: ICommand
    {
        /// <summary>
        /// Handles the specified command asynchronously.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">Token to observe for cancellation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleAsync(TCommand command, CancellationToken cancellationToken);
    }

    /// <summary>Handles returning commands of specified type.</summary>
    /// <typeparam name="TCommand">Handled command type.</typeparam>
    /// <typeparam name="TResult">Command result type.</typeparam>
    public interface ICommandHandler<TResult, in TCommand> : ICommandHandler
        where TCommand : ICommand<TResult>
        where TResult : IExecutionResult
    {
        /// <summary>
        /// Handles the specified command asynchronously and returns an execution result.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">Token to observe for cancellation.</param>
        /// <returns>The execution result produced by handling the command.</returns>
        Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
    }
}
