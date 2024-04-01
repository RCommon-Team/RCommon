using MediatR;
using Microsoft.Extensions.Logging;
using RCommon;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator;
using RCommon.Mediator.Subscribers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Mediator.MediatR
{
    public class TestRequestHandler : IAppRequestHandler<TestRequest>
    {
        public TestRequestHandler()
        {

        }

        public async Task HandleAsync(TestRequest request, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("{0} just handled this request {1}", new object[] { this.GetGenericTypeName(), request });
            await Task.CompletedTask;
        }
    }
}
