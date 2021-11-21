using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Messaging
{
    public class DistributedEventBroker : IDistributedEventBroker
    {
        private readonly IPublishEndpoint _pubishEndpoint;
        private List<IDistributedEvent> _distributedEvents;

        public DistributedEventBroker(IPublishEndpoint pubishEndpoint)
        {
            _pubishEndpoint = pubishEndpoint;
            this._distributedEvents = new List<IDistributedEvent>();
        }

        public void AddDistributedEvent(IDistributedEvent distributedEvent)
        {
            Guard.IsNotNull(distributedEvent, "distributedEvent");
            this._distributedEvents.Add(distributedEvent);
        }

        public void RemoveDistributedEvent(IDistributedEvent distributedEvent)
        {
            Guard.IsNotNull(distributedEvent, "distributedEvent");
            this._distributedEvents.Remove(distributedEvent);
        }

        public void ClearDistributedEvents()
        {
            this._distributedEvents.Clear();
        }

        public async Task PublishDistributedEvents(CancellationToken cancellationToken)
        {
            await _pubishEndpoint.PublishBatch(DistributedEvents, cancellationToken);
        }

        public IReadOnlyCollection<IDistributedEvent> DistributedEvents { get => _distributedEvents; }
    }
}
