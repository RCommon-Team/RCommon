using RCommon;
using RCommon.Mediator.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Mediator.MediatR
{
    public class TestRequestHandlerWithResponse : IAppRequestHandler<TestRequest, TestResponse>
    {
        public TestRequestHandlerWithResponse()
        {

        }

        public async Task<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            Console.WriteLine("{0} just handled this request {1}", new object[] { this.GetGenericTypeName(), request });

            var response = new TestResponse("Test Response Worked");
            return await Task.FromResult(response);
        }
    }
}
