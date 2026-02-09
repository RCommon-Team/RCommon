using RCommon.Models.ExecutionResults;

namespace RCommon.Models.Commands
{
    /// <summary>
    /// Represents the result of executing a command, wrapping an <see cref="IExecutionResult"/>
    /// to indicate whether the command succeeded or failed.
    /// </summary>
    /// <typeparam name="TExecutionResult">The type of <see cref="IExecutionResult"/> that conveys the execution outcome.</typeparam>
    /// <seealso cref="CommandResult{TExecutionResult}"/>
    /// <seealso cref="IExecutionResult"/>
    public interface ICommandResult<TExecutionResult> : IModel
        where TExecutionResult : IExecutionResult
    {
        /// <summary>
        /// Gets the execution result indicating the outcome of the command.
        /// </summary>
        TExecutionResult Result { get; }
    }
}
