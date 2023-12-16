using RCommon.ApplicationServices.ExecutionResults;

namespace RCommon.ApplicationServices.Commands
{
    public interface ICommandResult<TExecutionResult> where TExecutionResult : IExecutionResult
    {
        TExecutionResult Result { get; }
    }
}
