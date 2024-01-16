using EventFlow.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.EventSourcing.EventFlow
{
    public class MyAggregate : AggregateRoot<MyAggregate, MyId>, RCommon.Entities.Domain.IAggregateRoot
    {
        public MyState State { get; private set; }

        public MyAggregate(MyId id) : base(id)
        {
            State = new MyState();
            Register(State);
        }

        public void Count(int count)
        {
            Emit(new MyCountEvent(count));
        }

        public void AddDomainEvent(RCommon.Entities.Domain.IDomainEvent eventItem)
        {
            throw new NotImplementedException();
        }

        
    }
}
