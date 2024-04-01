﻿using MediatR;
using RCommon;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Mediator.MediatR
{
    public class TestRequestHandler : ISubscriber<TestObject>
    {
        public TestRequestHandler()
        {

        }

        public async Task HandleAsync(TestObject @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("{0} just handled this request {1}", new object[] { this.GetGenericTypeName(), @event });
            await Task.CompletedTask;
        }
    }
}