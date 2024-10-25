using RCommon.Models.ExecutionResults;

namespace RCommon.Models.Commands
{
    public interface ICommandResult<TExecutionResult> where TExecutionResult : IExecutionResult
    {
        TExecutionResult Result { get; }
    }
}
