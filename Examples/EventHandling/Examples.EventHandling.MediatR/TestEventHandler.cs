using MediatR;
using RCommon;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.EventHandling.MediatR
{
    public class TestEventHandler : ISubscriber<TestEvent>
    {
        public TestEventHandler()
        {
            
        }

        public async Task HandleAsync(TestEvent notification, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("{0} just handled this event {0}", new object[] { this.GetGenericTypeName(), notification });
            await Task.CompletedTask;
        }
    }
}
