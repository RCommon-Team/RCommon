using MediatR;
using RCommon.EventHandling;
using RCommon.Mediator;
using RCommon.Mediator.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Mediator.MediatR
{
    public class TestRequest : IAppRequest
    {
        public TestRequest(DateTime dateTime, Guid guid)
        {
            DateTime = dateTime;
            Guid = guid;
        }

        public TestRequest()
        {

        }

        public DateTime DateTime { get; }
        public Guid Guid { get; }
    }
}
