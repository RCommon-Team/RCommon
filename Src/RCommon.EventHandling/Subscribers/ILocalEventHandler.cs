
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Subscribers
{
    public interface ILocalEventHandler
    {

    }

    public interface ILocalEventHandler<TLocalEvent>
        where TLocalEvent : ILocalEvent
    {
        public Task Handle(TLocalEvent localEvent, CancellationToken cancellationToken);
    }
}
