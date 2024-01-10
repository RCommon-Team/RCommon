using RCommon.ApplicationServices.ExecutionResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Commands
{
    public interface ICommandHandler
    {
    }

    /// <summary>Handles commands of specified type.</summary>
    /// <typeparam name="TCommand">Handled command type.</typeparam>
    public interface ICommandHandler<in TCommand>
         where TCommand: ICommand
    {
        Task HandleAsync(TCommand command, CancellationToken cancellationToken);
    }

    /// <summary>Handles returning commands of specified type.</summary>
    /// <typeparam name="TCommand">Handled command type.</typeparam>
    /// <typeparam name="TResult">Command result type.</typeparam>
    public interface ICommandHandler<TResult, in TCommand>
        where TCommand : ICommand<TResult>
        where TResult : IExecutionResult
    {
        Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
    }
}
