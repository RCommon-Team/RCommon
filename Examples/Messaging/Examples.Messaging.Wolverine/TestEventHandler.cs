using RCommon.EventHandling.Subscribers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Messaging.Wolverine
{
    public class TestEventHandler : ISubscriber<TestEvent>
    {
        public TestEventHandler()
        {
                
        }


        public async Task HandleAsync(TestEvent notification, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("I just handled this event {0}", new object[] { notification.ToString() });
            await Task.CompletedTask;
        }
    }
}
