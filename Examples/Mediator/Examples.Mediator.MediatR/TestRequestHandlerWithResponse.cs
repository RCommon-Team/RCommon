using RCommon;
using RCommon.Mediator.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Mediator.MediatR
{
    public class TestRequestHandlerWithResponse : IAppRequestHandler<TestRequestWithResponse, TestResponse>
    {
        public TestRequestHandlerWithResponse()
        {

        }

        public async Task<TestResponse> HandleAsync(TestRequestWithResponse request, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("{0} just handled this request {1}", new object[] { this.GetGenericTypeName(), request });

            var response = new TestResponse("Test Response Worked");
            return await Task.FromResult(response);
        }
    }
}
