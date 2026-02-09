using RCommon.Models.ExecutionResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models.Commands
{
    /// <summary>
    /// Default implementation of <see cref="ICommandResult{TExecutionResult}"/> that wraps
    /// an execution result returned from a command handler.
    /// </summary>
    /// <typeparam name="TExecutionResult">The type of <see cref="IExecutionResult"/> conveying the command outcome.</typeparam>
    /// <remarks>
    /// This is a record type decorated with <see cref="DataContractAttribute"/> to support
    /// serialization across service boundaries.
    /// </remarks>
    [DataContract]
    public record CommandResult<TExecutionResult> : ICommandResult<TExecutionResult>
        where TExecutionResult : IExecutionResult
    {
        /// <inheritdoc />
        [DataMember]
        public TExecutionResult Result { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandResult{TExecutionResult}"/> record.
        /// </summary>
        /// <param name="result">The execution result representing the outcome of the command.</param>
        public CommandResult(TExecutionResult result)
        {
            Result = result;
        }
    }
}
