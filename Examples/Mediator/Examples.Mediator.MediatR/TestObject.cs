using MediatR;
using RCommon.EventHandling;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Mediator.MediatR
{
    public class TestObject : ISerializableEvent
    {
        public TestObject(DateTime dateTime, Guid guid)
        {
            DateTime = dateTime;
            Guid = guid;
        }

        public TestObject()
        {

        }

        public DateTime DateTime { get; }
        public Guid Guid { get; }
    }
}
