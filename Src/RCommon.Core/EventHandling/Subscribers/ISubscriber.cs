
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Subscribers
{

    public interface ISubscriber<TLocalEvent>
    {
        public Task HandleAsync(TLocalEvent localEvent, CancellationToken cancellationToken = default);
    }
}
