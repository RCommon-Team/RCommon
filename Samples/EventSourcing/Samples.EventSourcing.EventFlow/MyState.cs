using EventFlow.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.EventSourcing.EventFlow
{
    public class MyState : IEventApplier<MyAggregate, MyId>
    {
        public int Count { get; private set; }

        public bool Apply(MyAggregate aggregate, IAggregateEvent<MyAggregate, MyId> aggregateEvent)
        {
            var myCountEvent = (MyCountEvent)aggregateEvent;
            Count += myCountEvent.Count;
            return true;
        }
    }
}
