using RCommon.Models.ExecutionResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models.Commands
{
    [DataContract]
    public record CommandResult<TExecutionResult> : ICommandResult<TExecutionResult>
        where TExecutionResult : IExecutionResult
    {
        [DataMember]
        public TExecutionResult Result { get; }

        public CommandResult(TExecutionResult result)
        {
            Result = result;
        }
    }
}
