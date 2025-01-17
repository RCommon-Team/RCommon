﻿using RCommon.EventHandling;
using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.EventHandling.InMemoryEventBus
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
