using RCommon.ApplicationServices.ExecutionResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Commands
{
    public interface ICommand
    {
    }

    public interface ICommand<TResult> : ICommand
        where TResult : IExecutionResult
    {
    }
}
