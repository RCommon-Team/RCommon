using Wolverine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCommon.Models.Events;

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
