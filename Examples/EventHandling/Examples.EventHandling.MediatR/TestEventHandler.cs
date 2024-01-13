using MediatR;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.EventHandling.MediatR
{
    public class TestEventHandler : INotifierHandler<TestEvent>
    {
        public async Task HandleAsync(TestEvent notification, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("I just handled this event {0}", new object[] { notification.ToString() });
            await Task.CompletedTask;
        }
    }
}
