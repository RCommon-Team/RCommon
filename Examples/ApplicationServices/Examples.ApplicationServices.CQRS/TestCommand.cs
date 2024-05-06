using RCommon.ApplicationServices.Commands;
using RCommon.ApplicationServices.ExecutionResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.ApplicationServices.CQRS
{
    public class TestCommand : ICommand<IExecutionResult>
    {
        public TestCommand(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
