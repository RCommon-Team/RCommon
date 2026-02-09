using RCommon.Models.ExecutionResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models.Commands
{
    /// <summary>
    /// Marker interface representing a command in the CQRS pattern.
    /// Commands encapsulate intent to change the system state and are typically
    /// dispatched to a single handler for processing.
    /// </summary>
    /// <seealso cref="IModel"/>
    public interface ICommand : IModel
    {
    }

    /// <summary>
    /// Generic command interface that specifies the expected execution result type.
    /// Use this interface when a command handler should return a typed result indicating
    /// the outcome of the operation.
    /// </summary>
    /// <typeparam name="TResult">The type of <see cref="IExecutionResult"/> returned by the command handler.</typeparam>
    /// <seealso cref="ICommand"/>
    /// <seealso cref="IExecutionResult"/>
    public interface ICommand<TResult> : ICommand
        where TResult : IExecutionResult
    {
    }
}
