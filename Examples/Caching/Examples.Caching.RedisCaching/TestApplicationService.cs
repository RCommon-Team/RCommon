using RCommon.ApplicationServices.Commands;
using RCommon.ApplicationServices.ExecutionResults;
using RCommon.ApplicationServices.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Caching.RedisCaching
{
    public class TestApplicationService : ITestApplicationService
    {
        private readonly ICommandBus _commandBus;
        private readonly IQueryBus _queryBus;

        public TestApplicationService(ICommandBus commandBus, IQueryBus queryBus)
        {
            _commandBus = commandBus;
            _queryBus = queryBus;
        }

        public async Task<TestDto> ExecuteTestQuery(TestQuery query)
        {
            return await _queryBus.DispatchQueryAsync(query, CancellationToken.None);
        }

        public async Task<IExecutionResult> ExecuteTestCommand(TestCommand command)
        {
            return await _commandBus.DispatchCommandAsync(command, CancellationToken.None);
        }
    }
}
