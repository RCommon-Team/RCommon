using MediatR;
using RCommon.EventHandling;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.EventHandling.MediatR
{
    public class TestEvent : ISyncEvent, INotification
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
