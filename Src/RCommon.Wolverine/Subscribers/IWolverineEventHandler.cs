using Wolverine;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MassTransit.Subscribers
{
    public interface IWolverineEventHandler
    {
    }

    public interface IWolverineEventHandler<in TDistributedEvent> : IWolverineEventHandler
        where TDistributedEvent : class, ISerializableEvent
    {
        Task HandleAsync(TDistributedEvent distributedEvent, CancellationToken cancellationToken = default);
    }
}
