using RCommon;
using RCommon.ApplicationServices.Commands;
using RCommon.Models.ExecutionResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.ApplicationServices.CQRS
{
    public class TestCommandHandler : ICommandHandler<IExecutionResult, TestCommand>
    {
        public async Task<IExecutionResult> HandleAsync(TestCommand command, CancellationToken cancellationToken)
        {
            Console.WriteLine("{0} successfully handled {1}", new object[] { this.GetGenericTypeName(), command });
            return await Task.FromResult(new SuccessExecutionResult());
        }
    }
}
