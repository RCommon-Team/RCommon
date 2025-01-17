using MediatR;
using RCommon.Mediator;
using RCommon.Mediator.Subscribers;
using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.EventHandling.MediatR
{
    public class TestEvent : ISyncEvent
    {
        public TestEvent(DateTime dateTime, Guid guid)
        {
            DateTime = dateTime;
            Guid = guid;
        }

        public TestEvent()
        {
                
        }

        public DateTime DateTime { get; }
        public Guid Guid { get; }
    }
}
