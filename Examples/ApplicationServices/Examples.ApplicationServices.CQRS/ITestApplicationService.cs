using RCommon.Models.ExecutionResults;

namespace Examples.ApplicationServices.CQRS
{
    public interface ITestApplicationService
    {
        Task<IExecutionResult> ExecuteTestCommand(TestCommand command);
        Task<TestDto> ExecuteTestQuery(TestQuery query);
    }
}