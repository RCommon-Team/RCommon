using Microsoft.Extensions.Logging;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wolverine;

namespace RCommon.Wolverine.Producers
{
    public class SendWithWolverineEventProducer : IEventProducer
    {
        private readonly IMessageBus _messageBus;
        private readonly ILogger<SendWithWolverineEventProducer> _logger;

        public SendWithWolverineEventProducer(IMessageBus messageBus, ILogger<SendWithWolverineEventProducer> logger)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) where T : ISerializableEvent
        {
            Guard.IsNotNull(@event, nameof(@event));
            _logger.LogInformation("{0} sending event: {1}", new object[] { this.GetGenericTypeName(), @event });
            await _messageBus.SendAsync(@event);
        }
    }
}
