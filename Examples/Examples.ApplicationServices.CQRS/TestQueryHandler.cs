using RCommon;
using RCommon.ApplicationServices.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.ApplicationServices.CQRS
{
    public class TestQueryHandler : IQueryHandler<TestQuery, TestDto>
    {
        private readonly IQueryBus _queryBus;

        public TestQueryHandler(IQueryBus queryBus)
        {
            _queryBus = queryBus;
        }
        public async Task<TestDto> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("{0} successfully handled {1}", new object[] { this.GetGenericTypeName(), query });
            return await Task.FromResult(new TestDto("Success!"));
            
        }
    }
}
