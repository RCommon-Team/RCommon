using EventFlow.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.EventSourcing.EventFlow
{
    public class MyCountEvent : IAggregateEvent<MyAggregate, MyId>, RCommon.Entities.Domain.IDomainEvent
    {
        public int Count { get; private set; }

        public MyCountEvent(int count)
        {
            Count = count;
        }
    }
}
