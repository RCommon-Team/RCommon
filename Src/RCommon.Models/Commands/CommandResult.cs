using RCommon.Models.ExecutionResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models.Commands
{
    public class CommandResult<TExecutionResult> : ICommandResult<TExecutionResult>
        where TExecutionResult : IExecutionResult
    {
        public TExecutionResult Result { get; }

        public CommandResult(TExecutionResult result)
        {
            Result = result;
        }
    }
}
