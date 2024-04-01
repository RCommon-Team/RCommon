using RCommon.Mediator.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Mediator.MediatR
{
    public class TestRequestWithResponse : IAppRequest<TestResponse>
    {
        public TestRequestWithResponse(DateTime dateTime, Guid guid)
        {
            DateTime = dateTime;
            Guid = guid;
        }

        public DateTime DateTime { get; }
        public Guid Guid { get; }
    }
}
